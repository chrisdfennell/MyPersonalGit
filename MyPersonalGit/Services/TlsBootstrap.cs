using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Data.Sqlite;

namespace MyPersonalGit.Services;

public record TlsSettings(
    bool Enabled,
    int HttpsPort,
    bool HttpsRedirect,
    string CertSource, // "none", "file", "selfSigned"
    string CertPath,
    string KeyPath,
    string PfxPath,
    string PfxPassword
);

public static class TlsBootstrap
{
    private const string SelfSignedCertDir = "/data/tls";
    private const string SelfSignedCertPath = "/data/tls/selfsigned.pfx";

    /// <summary>
    /// Read TLS settings directly from the SQLite database before DI is available.
    /// Falls back to defaults if the database or table doesn't exist yet.
    /// </summary>
    public static TlsSettings ReadTlsSettings(IConfiguration config)
    {
        var defaults = new TlsSettings(false, 8443, false, "none", "", "", "", "");

        // Determine the database path from the connection string
        var connStr = config.GetConnectionString("DefaultConnection")
            ?? "Data Source=/data/mypersonalgit.db";

        // Check for a file-based database config override
        var dbConfigPath = Path.Combine(
            Environment.GetEnvironmentVariable("DATA_DIR") ?? "/data",
            "database.json"
        );
        if (File.Exists(dbConfigPath))
        {
            try
            {
                var json = File.ReadAllText(dbConfigPath);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("Provider", out var prov) && prov.GetString() == "sqlite")
                {
                    if (doc.RootElement.TryGetProperty("ConnectionString", out var cs))
                        connStr = cs.GetString() ?? connStr;
                }
                else if (doc.RootElement.TryGetProperty("Provider", out _))
                {
                    // Non-SQLite DB — can't read directly, return defaults
                    return defaults;
                }
            }
            catch { }
        }

        // Only handle SQLite for direct reads
        if (!connStr.Contains("Data Source", StringComparison.OrdinalIgnoreCase))
            return defaults;

        try
        {
            using var conn = new SqliteConnection(connStr);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT ""EnableHttps"", ""HttpsPort"", ""HttpsRedirect"",
                ""TlsCertSource"", ""TlsCertPath"", ""TlsKeyPath"", ""TlsPfxPath"", ""TlsPfxPassword""
                FROM ""SystemSettings"" LIMIT 1";

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new TlsSettings(
                    Enabled: reader.GetInt64(0) != 0,
                    HttpsPort: (int)reader.GetInt64(1),
                    HttpsRedirect: reader.GetInt64(2) != 0,
                    CertSource: reader.GetString(3),
                    CertPath: reader.GetString(4),
                    KeyPath: reader.GetString(5),
                    PfxPath: reader.GetString(6),
                    PfxPassword: reader.GetString(7)
                );
            }
        }
        catch
        {
            // Table/columns may not exist yet on first run
        }

        return defaults;
    }

    /// <summary>
    /// Load or generate the X509 certificate based on the configured source.
    /// </summary>
    public static X509Certificate2? LoadCertificate(TlsSettings settings)
    {
        if (!settings.Enabled) return null;

        return settings.CertSource switch
        {
            "file" => LoadFromFile(settings),
            "selfSigned" => LoadOrGenerateSelfSigned(),
            _ => null
        };
    }

    private static X509Certificate2? LoadFromFile(TlsSettings settings)
    {
        // PFX file (single file with cert + key)
        if (!string.IsNullOrEmpty(settings.PfxPath) && File.Exists(settings.PfxPath))
        {
            return string.IsNullOrEmpty(settings.PfxPassword)
                ? X509CertificateLoader.LoadPkcs12FromFile(settings.PfxPath, null)
                : X509CertificateLoader.LoadPkcs12FromFile(settings.PfxPath, settings.PfxPassword);
        }

        // PEM cert + key files (e.g., Let's Encrypt)
        if (!string.IsNullOrEmpty(settings.CertPath) && !string.IsNullOrEmpty(settings.KeyPath)
            && File.Exists(settings.CertPath) && File.Exists(settings.KeyPath))
        {
            return X509Certificate2.CreateFromPemFile(settings.CertPath, settings.KeyPath);
        }

        Console.WriteLine("Warning: TLS cert source is 'file' but no valid cert files found.");
        return null;
    }

    private static X509Certificate2 LoadOrGenerateSelfSigned()
    {
        // Reuse existing self-signed cert if it exists and hasn't expired
        if (File.Exists(SelfSignedCertPath))
        {
            try
            {
                var existing = X509CertificateLoader.LoadPkcs12FromFile(SelfSignedCertPath, "mypersonalgit-selfsigned");
                if (existing.NotAfter > DateTime.UtcNow.AddDays(7))
                {
                    Console.WriteLine($"==> Using existing self-signed certificate (expires {existing.NotAfter:yyyy-MM-dd})");
                    return existing;
                }
                Console.WriteLine("==> Self-signed certificate is expiring soon, regenerating...");
            }
            catch
            {
                Console.WriteLine("==> Existing self-signed certificate is invalid, regenerating...");
            }
        }

        return GenerateSelfSignedCert();
    }

    /// <summary>
    /// Generate a new self-signed X509 certificate and persist it to disk.
    /// </summary>
    public static X509Certificate2 GenerateSelfSignedCert()
    {
        Directory.CreateDirectory(SelfSignedCertDir);

        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=MyPersonalGit Self-Signed, O=MyPersonalGit",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );

        // Add Subject Alternative Names for common local access patterns
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddDnsName("*");
        sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback);
        sanBuilder.AddIpAddress(System.Net.IPAddress.IPv6Loopback);

        // Try to add the machine hostname
        try { sanBuilder.AddDnsName(Environment.MachineName); } catch { }

        request.CertificateExtensions.Add(sanBuilder.Build());
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                false));
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, // Server Authentication
                false));

        var cert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(2)
        );

        // Export as PFX and save
        var pfxBytes = cert.Export(X509ContentType.Pfx, "mypersonalgit-selfsigned");
        File.WriteAllBytes(SelfSignedCertPath, pfxBytes);

        Console.WriteLine($"==> Generated self-signed TLS certificate (expires {cert.NotAfter:yyyy-MM-dd})");
        Console.WriteLine($"==> Saved to {SelfSignedCertPath}");

        // Return a new instance loaded from the PFX (ensures private key is usable on all platforms)
        return X509CertificateLoader.LoadPkcs12(pfxBytes, "mypersonalgit-selfsigned");
    }
}
