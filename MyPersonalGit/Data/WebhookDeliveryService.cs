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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookDeliveryService> _logger;

    public WebhookDeliveryService(IDbContextFactory<AppDbContext> dbFactory, IHttpClientFactory httpClientFactory, ILogger<WebhookDeliveryService> logger)
    {
        _dbFactory = dbFactory;
        _httpClientFactory = httpClientFactory;
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
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);

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
            var response = await client.SendAsync(request);
            statusCode = (int)response.StatusCode;
            responseBody = await response.Content.ReadAsStringAsync();
            success = response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            responseBody = ex.Message;
            statusCode = 0;
        }

        // Record delivery
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

        // Update last triggered
        var wh = await db.Webhooks.FindAsync(webhook.Id);
        if (wh != null)
            wh.LastTriggeredAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        _logger.LogInformation("Webhook delivered: {EventType} to {Url} - Status: {StatusCode}", eventType, webhook.Url, statusCode);
    }

    private static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLower();
    }
}
