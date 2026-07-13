using Docker.DotNet;
using Docker.DotNet.Models;

namespace MyPersonalGit.Data;

// Helper mode, entered from Program.cs when MPG_UPDATE_TARGET is set. This process
// lives in a short-lived container spawned by SelfUpdateService.StartUpdateAsync
// (running the freshly pulled image) and replaces the target app container with one
// created from MPG_UPDATE_IMAGE, preserving its name, ports, volumes, and environment.
// If anything goes wrong after the old container is stopped, it is rolled back.
public static class SelfUpdater
{
    public static async Task<int> RunAsync()
    {
        var targetId = Environment.GetEnvironmentVariable("MPG_UPDATE_TARGET")!;
        var image = Environment.GetEnvironmentVariable("MPG_UPDATE_IMAGE");
        if (string.IsNullOrEmpty(image))
        {
            Console.WriteLine("Self-update: MPG_UPDATE_IMAGE not set — aborting.");
            return 1;
        }

        var docker = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();

        var inspect = await docker.Containers.InspectContainerAsync(targetId);
        var name = inspect.Name.TrimStart('/');
        Console.WriteLine($"Self-update: replacing container '{name}' ({targetId[..12]}) with {image}");

        // Stop the old container first so its name and ports are free for the new one
        await docker.Containers.StopContainerAsync(targetId, new ContainerStopParameters { WaitBeforeKillSeconds = 30 });
        await docker.Containers.RenameContainerAsync(targetId,
            new ContainerRenameParameters { NewName = $"{name}-old-{targetId[..8]}" }, CancellationToken.None);

        string? newId = null;
        try
        {
            // Our helper-mode variables must not leak into the real app container
            var env = (inspect.Config.Env ?? new List<string>())
                .Where(e => !e.StartsWith("MPG_UPDATE_")).ToList();

            // Recreate custom network attachments; dynamic fields (IPs, old aliases) stay behind
            IDictionary<string, EndpointSettings>? endpoints = null;
            if (inspect.NetworkSettings?.Networks is { Count: > 0 } networks)
            {
                endpoints = networks.ToDictionary(
                    kv => kv.Key,
                    kv => new EndpointSettings { NetworkID = kv.Value.NetworkID });
            }

            var created = await docker.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Name = name,
                Image = image,
                Env = env,
                Cmd = inspect.Config.Cmd,
                Entrypoint = inspect.Config.Entrypoint,
                Labels = inspect.Config.Labels,
                ExposedPorts = inspect.Config.ExposedPorts,
                WorkingDir = inspect.Config.WorkingDir,
                User = inspect.Config.User,
                HostConfig = inspect.HostConfig,
                NetworkingConfig = endpoints == null ? null : new NetworkingConfig { EndpointsConfig = endpoints }
            });
            newId = created.ID;
            await docker.Containers.StartContainerAsync(newId, new ContainerStartParameters());

            // The new container is up — the old one can go
            await docker.Containers.RemoveContainerAsync(targetId, new ContainerRemoveParameters { Force = true });
            Console.WriteLine($"Self-update: '{name}' is now running {image}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Self-update failed: {ex.Message} — rolling back to the previous container");
            try
            {
                if (newId != null)
                    await docker.Containers.RemoveContainerAsync(newId, new ContainerRemoveParameters { Force = true });
                await docker.Containers.RenameContainerAsync(targetId,
                    new ContainerRenameParameters { NewName = name }, CancellationToken.None);
                await docker.Containers.StartContainerAsync(targetId, new ContainerStartParameters());
                Console.WriteLine("Self-update: rollback succeeded — previous version is running again");
            }
            catch (Exception rollbackEx)
            {
                Console.WriteLine($"Self-update: rollback failed: {rollbackEx.Message}");
            }
            return 1;
        }
    }
}
