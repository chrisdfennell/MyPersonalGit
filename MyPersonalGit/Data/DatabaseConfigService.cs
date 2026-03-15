using System.Text.Json;

namespace MyPersonalGit.Data;

public class DatabaseConfig
{
    public string Provider { get; set; } = "sqlite";
    public string ConnectionString { get; set; } = "Data Source=mypersonalgit.db";
}

public interface IDatabaseConfigService
{
    DatabaseConfig GetCurrentConfig();
    void SaveConfig(DatabaseConfig config);
    string GetConfigFilePath();
}

/// <summary>
/// Manages the database configuration file (database.json) stored in the data directory.
/// This file is read at startup to determine which DB provider to use.
/// Changes require a restart to take effect.
/// </summary>
public class DatabaseConfigService : IDatabaseConfigService
{
    private readonly IConfiguration _configuration;
    private readonly string _configPath;

    public DatabaseConfigService(IConfiguration configuration)
    {
        _configuration = configuration;

        var dataDir = configuration["Ssh:DataDir"]
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".mypersonalgit");
        var baseDir = Path.GetDirectoryName(dataDir) ?? dataDir;

        // Store database.json alongside the SSH data directory
        _configPath = Path.Combine(baseDir, "database.json");
    }

    public string GetConfigFilePath() => _configPath;

    public DatabaseConfig GetCurrentConfig()
    {
        // Read from the config file if it exists
        if (File.Exists(_configPath))
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                var config = JsonSerializer.Deserialize<DatabaseConfig>(json);
                if (config != null) return config;
            }
            catch { }
        }

        // Fall back to what's in appsettings / env vars (the active config)
        return new DatabaseConfig
        {
            Provider = _configuration["Database:Provider"] ?? "sqlite",
            ConnectionString = _configuration.GetConnectionString("Default") ?? "Data Source=mypersonalgit.db"
        };
    }

    public void SaveConfig(DatabaseConfig config)
    {
        var dir = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}
