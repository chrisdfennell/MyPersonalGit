using LibGit2Sharp;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MyPersonalGit.Data;

public class WorkflowDefinition
{
    public string Name { get; set; } = "";
    public string FileName { get; set; } = "";
    public object? On { get; set; }
    public Dictionary<string, string>? Env { get; set; }
    public Dictionary<string, JobDefinition> Jobs { get; set; } = new();
    public List<WorkflowInput> Inputs { get; set; } = new();
}

public class WorkflowInput
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Required { get; set; }
    public string? Default { get; set; }
    public string Type { get; set; } = "string"; // string, boolean, choice, number
    public List<string> Options { get; set; } = new();
}

public class JobDefinition
{
    public string RunsOn { get; set; } = "ubuntu-latest";
    public List<string> Needs { get; set; } = new();
    public int? TimeoutMinutes { get; set; }
    public Dictionary<string, List<string>>? Matrix { get; set; }
    public bool FailFast { get; set; } = true;
    public Dictionary<string, string>? Env { get; set; }
    public List<StepDefinition> Steps { get; set; } = new();
}

public class StepDefinition
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? If { get; set; }
    public int? TimeoutMinutes { get; set; }
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

            // Parse workflow_dispatch inputs
            ParseWorkflowDispatchInputs(def);

            // Workflow-level env
            if (raw.ContainsKey("env") && raw["env"] is Dictionary<object, object> wfEnvDict)
                def.Env = wfEnvDict.ToDictionary(k => k.Key.ToString()!, v => v.Value?.ToString() ?? "");

            if (!raw.ContainsKey("jobs")) return def;

            var jobsRaw = raw["jobs"];
            if (jobsRaw is not Dictionary<object, object> jobsDict) return def;

            foreach (var (jobKey, jobValue) in jobsDict)
            {
                if (jobValue is not Dictionary<object, object> jobObj) continue;

                var jobDef = new JobDefinition();

                if (jobObj.ContainsKey("runs-on"))
                    jobDef.RunsOn = jobObj["runs-on"]?.ToString() ?? "ubuntu-latest";

                // needs: (string or list)
                if (jobObj.ContainsKey("needs"))
                {
                    var needsVal = jobObj["needs"];
                    if (needsVal is string needsStr)
                        jobDef.Needs.Add(needsStr);
                    else if (needsVal is List<object> needsList)
                        jobDef.Needs.AddRange(needsList
                            .Select(n => n?.ToString() ?? "")
                            .Where(n => !string.IsNullOrEmpty(n)));
                }

                // timeout-minutes
                if (jobObj.ContainsKey("timeout-minutes") && int.TryParse(jobObj["timeout-minutes"]?.ToString(), out var jobTimeout))
                    jobDef.TimeoutMinutes = jobTimeout;

                // strategy.matrix and strategy.fail-fast
                if (jobObj.ContainsKey("strategy") && jobObj["strategy"] is Dictionary<object, object> strategyObj)
                {
                    if (strategyObj.ContainsKey("fail-fast"))
                        jobDef.FailFast = strategyObj["fail-fast"]?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? true;

                    if (strategyObj.ContainsKey("matrix") && strategyObj["matrix"] is Dictionary<object, object> matrixObj)
                    {
                        jobDef.Matrix = new Dictionary<string, List<string>>();
                        foreach (var (mk, mv) in matrixObj)
                        {
                            var key = mk.ToString()!;
                            if (mv is List<object> values)
                                jobDef.Matrix[key] = values.Select(v => v?.ToString() ?? "").ToList();
                        }
                    }
                }

                // Job-level env
                if (jobObj.ContainsKey("env") && jobObj["env"] is Dictionary<object, object> jobEnvDict)
                    jobDef.Env = jobEnvDict.ToDictionary(k => k.Key.ToString()!, v => v.Value?.ToString() ?? "");

                if (jobObj.ContainsKey("steps") && jobObj["steps"] is List<object> stepsList)
                {
                    foreach (var stepObj in stepsList)
                    {
                        if (stepObj is not Dictionary<object, object> stepDict) continue;

                        var step = new StepDefinition
                        {
                            Id   = stepDict.ContainsKey("id")   ? stepDict["id"]?.ToString()   : null,
                            Name = stepDict.ContainsKey("name") ? stepDict["name"]?.ToString() : null,
                            If   = stepDict.ContainsKey("if")   ? stepDict["if"]?.ToString()   : null,
                            Run  = stepDict.ContainsKey("run")  ? stepDict["run"]?.ToString()  : null,
                            Uses = stepDict.ContainsKey("uses") ? stepDict["uses"]?.ToString() : null
                        };

                        if (stepDict.ContainsKey("timeout-minutes") && int.TryParse(stepDict["timeout-minutes"]?.ToString(), out var stepTimeout))
                            step.TimeoutMinutes = stepTimeout;

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

    private static void ParseWorkflowDispatchInputs(WorkflowDefinition def)
    {
        if (def.On is not Dictionary<object, object> onDict) return;

        var dispatchKey = onDict.Keys.FirstOrDefault(k =>
            k.ToString()?.Equals("workflow_dispatch", StringComparison.OrdinalIgnoreCase) == true);
        if (dispatchKey == null) return;

        if (onDict[dispatchKey] is not Dictionary<object, object> dispatchObj) return;

        var inputsKey = dispatchObj.Keys.FirstOrDefault(k =>
            k.ToString()?.Equals("inputs", StringComparison.OrdinalIgnoreCase) == true);
        if (inputsKey == null) return;

        if (dispatchObj[inputsKey] is not Dictionary<object, object> inputsDict) return;

        foreach (var (inputName, inputValue) in inputsDict)
        {
            var input = new WorkflowInput { Name = inputName.ToString()! };

            if (inputValue is Dictionary<object, object> inputObj)
            {
                if (inputObj.ContainsKey("description"))
                    input.Description = inputObj["description"]?.ToString() ?? "";
                if (inputObj.ContainsKey("required"))
                    input.Required = inputObj["required"]?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
                if (inputObj.ContainsKey("default"))
                    input.Default = inputObj["default"]?.ToString();
                if (inputObj.ContainsKey("type"))
                    input.Type = inputObj["type"]?.ToString() ?? "string";
                if (inputObj.ContainsKey("options") && inputObj["options"] is List<object> opts)
                    input.Options = opts.Select(o => o?.ToString() ?? "").ToList();
            }

            def.Inputs.Add(input);
        }
    }
}
