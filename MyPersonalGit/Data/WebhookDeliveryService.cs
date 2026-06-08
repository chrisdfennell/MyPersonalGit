using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IWebhookDeliveryService
{
    Task TriggerWebhooksAsync(string repoName, string eventType, object payload);
}

public class WebhookDeliveryService : IWebhookDeliveryService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<WebhookDeliveryService> _logger;

    // No auto-redirect: stops an attacker-controlled endpoint from 30x-redirecting the
    // request to an internal address after the initial SSRF check passes.
    private static readonly HttpClient _httpClient = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        ConnectTimeout = TimeSpan.FromSeconds(5)
    })
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public WebhookDeliveryService(IDbContextFactory<AppDbContext> dbFactory, ILogger<WebhookDeliveryService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task TriggerWebhooksAsync(string repoName, string eventType, object payload)
    {
        using var db = _dbFactory.CreateDbContext();
        var webhooks = await db.Webhooks
            .Where(w => w.RepoName.ToLower() == repoName.ToLower() && w.IsActive && w.Events.Contains(eventType))
            .ToListAsync();

        if (!webhooks.Any()) return;

        var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });

        foreach (var webhook in webhooks)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await DeliverWebhookAsync(webhook, eventType, payloadJson);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Webhook delivery failed for {WebhookId} to {Url}", webhook.Id, webhook.Url);
                }
            });
        }
    }

    private async Task DeliverWebhookAsync(Webhook webhook, string eventType, string payloadJson)
    {
        // SSRF guard: refuse to deliver to non-public destinations (loopback/private/
        // link-local/reserved), which would let a webhook reach internal services or
        // cloud metadata and exfiltrate the response via the stored delivery record.
        if (!await IsDeliverableUrlAsync(webhook.Url))
        {
            await RecordDeliveryAsync(webhook, eventType, payloadJson, 0,
                "Blocked: webhook URL is not a public address (loopback/private/link-local/reserved).", false);
            _logger.LogWarning("Blocked webhook {WebhookId} to non-public URL {Url}", webhook.Id, webhook.Url);
            return;
        }

        // Compute HMAC-SHA256 signature
        var signature = ComputeSignature(payloadJson, webhook.Secret);

        var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url);
        request.Headers.Add("X-PersonalGit-Event", eventType);
        request.Headers.Add("X-PersonalGit-Signature", $"sha256={signature}");
        request.Headers.Add("X-PersonalGit-Delivery", Guid.NewGuid().ToString());
        request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

        int statusCode = 0;
        string? responseBody = null;
        bool success = false;

        try
        {
            var response = await _httpClient.SendAsync(request);
            statusCode = (int)response.StatusCode;
            responseBody = await response.Content.ReadAsStringAsync();
            success = response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            responseBody = ex.Message;
            statusCode = 0;
        }

        await RecordDeliveryAsync(webhook, eventType, payloadJson, statusCode, responseBody, success);
        _logger.LogInformation("Webhook delivered: {EventType} to {Url} - Status: {StatusCode}", eventType, webhook.Url, statusCode);
    }

    private async Task RecordDeliveryAsync(Webhook webhook, string eventType, string payloadJson, int statusCode, string? responseBody, bool success)
    {
        using var db = _dbFactory.CreateDbContext();
        db.WebhookDeliveries.Add(new WebhookDelivery
        {
            WebhookId = webhook.Id,
            Event = eventType,
            Payload = payloadJson.Length > 10000 ? payloadJson[..10000] : payloadJson,
            StatusCode = statusCode,
            Response = responseBody?.Length > 5000 ? responseBody[..5000] : responseBody,
            DeliveredAt = DateTime.UtcNow,
            Success = success
        });

        var wh = await db.Webhooks.FindAsync(webhook.Id);
        if (wh != null)
            wh.LastTriggeredAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    // ── SSRF protection ──────────────────────────────────────────────
    // Only allow http/https to a host that resolves entirely to public IP addresses.
    private static async Task<bool> IsDeliverableUrlAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost);
        }
        catch
        {
            return false;
        }

        // Refuse if there are no addresses or ANY resolved address is non-public
        // (defends against split-horizon DNS returning a mix).
        return addresses.Length > 0 && addresses.All(ip => !IsPrivateOrReserved(ip));
    }

    internal static bool IsPrivateOrReserved(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return true;

        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            return b[0] == 0                                   // 0.0.0.0/8
                || b[0] == 10                                  // 10/8 private
                || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)  // 100.64/10 CGNAT
                || b[0] == 127                                 // loopback
                || (b[0] == 169 && b[1] == 254)                // 169.254/16 link-local (incl. cloud metadata 169.254.169.254)
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)   // 172.16/12 private
                || (b[0] == 192 && b[1] == 168)                // 192.168/16 private
                || b[0] >= 224;                                // 224/4 multicast + 240/4 reserved
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast)
                return true;
            var b = ip.GetAddressBytes();
            return (b[0] & 0xFE) == 0xFC;                      // fc00::/7 unique local
        }

        return true; // unknown address family — deny
    }

    private static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLower();
    }
}
