using LibGit2Sharp;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MyPersonalGit.Data;

public class WorkflowDefinition
{
    public string Name { get; set; } = "";
    public string FileName { get; set; } = "";
    public object? On { get; set; }
    public Dictionary<string, JobDefinition> Jobs { get; set; } = new();
}

public class JobDefinition
{
    public string RunsOn { get; set; } = "ubuntu-latest";
    public List<StepDefinition> Steps { get; set; } = new();
}

public class StepDefinition
{
    public string? Name { get; set; }
    public string? Run { get; set; }
    public string? Uses { get; set; }
    public Dictionary<string, string>? With { get; set; }
    public Dictionary<string, string>? Env { get; set; }
}

public class WorkflowYamlParser
{
    private static readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public List<WorkflowDefinition> ParseFromRepo(string repoPath)
    {
        var workflows = new List<WorkflowDefinition>();

        if (!Repository.IsValid(repoPath)) return workflows;

        try
        {
            using var repo = new Repository(repoPath);
            var head = repo.Head;
            if (head?.Tip == null) return workflows;

            // Look for .github/workflows directory in the tree
            var githubEntry = head.Tip[".github/workflows"];
            if (githubEntry == null || githubEntry.TargetType != TreeEntryTargetType.Tree)
                return workflows;

            var tree = (Tree)githubEntry.Target;
            foreach (var entry in tree)
            {
                if (entry.TargetType != TreeEntryTargetType.Blob) continue;
                var name = entry.Name.ToLowerInvariant();
                if (!name.EndsWith(".yml") && !name.EndsWith(".yaml")) continue;

                try
                {
                    var blob = (Blob)entry.Target;
                    using var reader = new StreamReader(blob.GetContentStream());
                    var yaml = reader.ReadToEnd();
                    var workflow = ParseYaml(yaml, entry.Name);
                    if (workflow != null)
                        workflows.Add(workflow);
                }
                catch { }
            }
        }
        catch { }

        return workflows;
    }

    private WorkflowDefinition? ParseYaml(string yaml, string fileName)
    {
        try
        {
            var raw = _deserializer.Deserialize<Dictionary<string, object?>>(yaml);
            if (raw == null) return null;

            var def = new WorkflowDefinition
            {
                FileName = fileName,
                Name = raw.ContainsKey("name") ? raw["name"]?.ToString() ?? fileName : fileName,
                On = raw.ContainsKey("on") ? raw["on"] : null
            };

            if (!raw.ContainsKey("jobs")) return def;

            var jobsRaw = raw["jobs"];
            if (jobsRaw is not Dictionary<object, object> jobsDict) return def;

            foreach (var (jobKey, jobValue) in jobsDict)
            {
                if (jobValue is not Dictionary<object, object> jobObj) continue;

                var jobDef = new JobDefinition();

                if (jobObj.ContainsKey("runs-on"))
                    jobDef.RunsOn = jobObj["runs-on"]?.ToString() ?? "ubuntu-latest";

                if (jobObj.ContainsKey("steps") && jobObj["steps"] is List<object> stepsList)
                {
                    foreach (var stepObj in stepsList)
                    {
                        if (stepObj is not Dictionary<object, object> stepDict) continue;

                        var step = new StepDefinition
                        {
                            Name = stepDict.ContainsKey("name") ? stepDict["name"]?.ToString() : null,
                            Run = stepDict.ContainsKey("run") ? stepDict["run"]?.ToString() : null,
                            Uses = stepDict.ContainsKey("uses") ? stepDict["uses"]?.ToString() : null
                        };

                        if (stepDict.ContainsKey("with") && stepDict["with"] is Dictionary<object, object> withDict)
                            step.With = withDict.ToDictionary(k => k.Key.ToString()!, v => v.Value?.ToString() ?? "");

                        if (stepDict.ContainsKey("env") && stepDict["env"] is Dictionary<object, object> envDict)
                            step.Env = envDict.ToDictionary(k => k.Key.ToString()!, v => v.Value?.ToString() ?? "");

                        jobDef.Steps.Add(step);
                    }
                }

                def.Jobs[jobKey.ToString()!] = jobDef;
            }

            return def;
        }
        catch
        {
            return null;
        }
    }
}
