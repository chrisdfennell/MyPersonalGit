using System.Text.Json;
using System.Text.RegularExpressions;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace MyPersonalGit.Data;

public record UpdateCheckResult(string CurrentVersion, string? LatestVersion, bool UpdateAvailable, string? Error = null);

public interface ISelfUpdateService
{
    string CurrentVersion { get; }
    Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken ct = default);
    Task<(bool Success, string Message)> StartUpdateAsync(string versionTag);
}

public class SelfUpdateService : ISelfUpdateService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<SelfUpdateService> _logger;
    private readonly string _imageRepo;
    private readonly DockerClient? _docker;

    public SelfUpdateService(IHttpClientFactory httpFactory, ILogger<SelfUpdateService> logger, IConfiguration config)
    {
        _httpFactory = httpFactory;
        _logger = logger;
        _imageRepo = config["Update:Image"] ?? "fennch/mypersonalgit";

        try
        {
            var dockerUri = OperatingSystem.IsWindows()
                ? new Uri("npipe://./pipe/docker_engine")
                : new Uri("unix:///var/run/docker.sock");
            _docker = new DockerClientConfiguration(dockerUri).CreateClient();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Docker client initialization failed — self-update disabled");
        }
    }

    public string CurrentVersion => Environment.GetEnvironmentVariable("APP_VERSION") ?? "dev";

    public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken ct = default)
    {
        try
        {
            var http = _httpFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(15);
            var json = await http.GetStringAsync($"https://hub.docker.com/v2/repositories/{_imageRepo}/tags?page_size=100", ct);
            using var doc = JsonDocument.Parse(json);
            var names = doc.RootElement.GetProperty("results").EnumerateArray()
                .Select(r => r.GetProperty("name").GetString())
                .OfType<string>()
                .ToList();

            var latest = LatestVersionTag(names);
            if (latest == null)
                return new UpdateCheckResult(CurrentVersion, null, false, "No version tags found on Docker Hub.");

            return new UpdateCheckResult(CurrentVersion, latest, IsNewer(CurrentVersion, latest));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update check against Docker Hub failed");
            return new UpdateCheckResult(CurrentVersion, null, false, $"Update check failed: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> StartUpdateAsync(string versionTag)
    {
        if (_docker == null)
            return (false, "Docker is not available. Self-update requires running in Docker with /var/run/docker.sock mounted.");

        var imageRef = $"{_imageRepo}:{versionTag}";
        try
        {
            // Inside a container the hostname is the short container id — use it to find ourselves
            var hostname = Environment.MachineName;
            var containers = await _docker.Containers.ListContainersAsync(new ContainersListParameters());
            var self = containers.FirstOrDefault(c => c.ID.StartsWith(hostname, StringComparison.OrdinalIgnoreCase));
            if (self == null)
                return (false, "Could not identify the running container. Self-update only works when the app itself runs in Docker.");

            _logger.LogInformation("Self-update: pulling {Image}", imageRef);
            await _docker.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = _imageRepo, Tag = versionTag },
                null, new Progress<JSONMessage>());

            // A container can't stop and recreate itself, so hand off to a short-lived
            // helper running the image we just pulled. It swaps the app container for the
            // new version and exits; AutoRemove cleans it up. See SelfUpdater.RunAsync.
            var helper = await _docker.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = imageRef,
                Name = $"mypersonalgit-updater-{Guid.NewGuid().ToString("N")[..8]}",
                Env = new List<string>
                {
                    $"MPG_UPDATE_TARGET={self.ID}",
                    $"MPG_UPDATE_IMAGE={imageRef}"
                },
                HostConfig = new HostConfig
                {
                    AutoRemove = true,
                    Binds = new List<string> { "/var/run/docker.sock:/var/run/docker.sock" }
                }
            });
            await _docker.Containers.StartContainerAsync(helper.ID, new ContainerStartParameters());

            _logger.LogInformation("Self-update to {Image} started; helper container {Helper} will replace this container", imageRef, helper.ID);
            return (true, $"Update to {versionTag} started — the server will restart in a moment.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Self-update to {Image} failed to start", imageRef);
            return (false, $"Update failed to start: {ex.Message}");
        }
    }

    // Highest semver-style tag (e.g. "v1.16.2") wins; non-version tags like "latest" are ignored
    public static string? LatestVersionTag(IEnumerable<string> tagNames) =>
        tagNames.Select(n => (Name: n, Version: ParseVersionTag(n)))
            .Where(t => t.Version != null)
            .OrderByDescending(t => t.Version)
            .Select(t => t.Name)
            .FirstOrDefault();

    // A dev/unparseable current version never claims an update is available
    public static bool IsNewer(string current, string candidate)
    {
        var cur = ParseVersionTag(current);
        var cand = ParseVersionTag(candidate);
        return cur != null && cand != null && cand > cur;
    }

    private static System.Version? ParseVersionTag(string tag)
    {
        var m = Regex.Match(tag.Trim(), @"^v?(\d+\.\d+(\.\d+){0,2})$");
        return m.Success && System.Version.TryParse(m.Groups[1].Value, out var v) ? v : null;
    }
}
