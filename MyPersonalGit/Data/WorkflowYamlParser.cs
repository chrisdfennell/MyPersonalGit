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
    public string? DefaultWorkingDirectory { get; set; }
    public string? DefaultShell { get; set; }
    public Dictionary<string, JobDefinition> Jobs { get; set; } = new();
    public List<WorkflowInput> Inputs { get; set; } = new();
    public WorkflowCallDefinition? WorkflowCall { get; set; }
    public bool IsReusable { get; set; }
}

public class WorkflowInput
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Required { get; set; }
    public string? Default { get; set; }
    public string Type { get; set; } = "string";
    public List<string> Options { get; set; } = new();
}

public class WorkflowCallDefinition
{
    public Dictionary<string, WorkflowInput> Inputs { get; set; } = new();
    public Dictionary<string, WorkflowCallOutput> Outputs { get; set; } = new();
    public Dictionary<string, WorkflowCallSecret> Secrets { get; set; } = new();
}

public class WorkflowCallOutput
{
    public string Description { get; set; } = "";
    public string Value { get; set; } = "";
}

public class WorkflowCallSecret
{
    public string Description { get; set; } = "";
    public bool Required { get; set; }
}

public class JobDefinition
{
    public string RunsOn { get; set; } = "ubuntu-latest";
    public string? If { get; set; }
    public List<string> Needs { get; set; } = new();
    public int? TimeoutMinutes { get; set; }
    public Dictionary<string, List<string>>? Matrix { get; set; }
    public bool FailFast { get; set; } = true;
    public int? MaxParallel { get; set; }
    public Dictionary<string, string>? Outputs { get; set; }
    public Dictionary<string, string>? Env { get; set; }
    public List<StepDefinition> Steps { get; set; } = new();
    public string? Environment { get; set; }
    public string? EnvironmentUrl { get; set; }
    public string? Uses { get; set; }
    public Dictionary<string, string>? With { get; set; }
    public Dictionary<string, string>? Secrets { get; set; }
}

public class StepDefinition
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? If { get; set; }
    public bool ContinueOnError { get; set; }
    public int? TimeoutMinutes { get; set; }
    public string? WorkingDirectory { get; set; }
    public string? Shell { get; set; }
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

            ParseWorkflowDispatchInputs(def);
            ParseWorkflowCallDefinition(def);

            // Workflow-level env
            if (raw.ContainsKey("env") && raw["env"] is Dictionary<object, object> wfEnvDict)
                def.Env = wfEnvDict.ToDictionary(k => k.Key.ToString()!, v => v.Value?.ToString() ?? "");

            // defaults.run.working-directory
            if (raw.ContainsKey("defaults") && raw["defaults"] is Dictionary<object, object> defaultsObj)
            {
                if (defaultsObj.ContainsKey("run") && defaultsObj["run"] is Dictionary<object, object> runObj)
                {
                    if (runObj.ContainsKey("working-directory"))
                        def.DefaultWorkingDirectory = runObj["working-directory"]?.ToString();
                    if (runObj.ContainsKey("shell"))
                        def.DefaultShell = runObj["shell"]?.ToString();
                }
            }

            if (!raw.ContainsKey("jobs")) return def;

            var jobsRaw = raw["jobs"];
            if (jobsRaw is not Dictionary<object, object> jobsDict) return def;

            foreach (var (jobKey, jobValue) in jobsDict)
            {
                if (jobValue is not Dictionary<object, object> jobObj) continue;

                var jobDef = new JobDefinition();

                if (jobObj.ContainsKey("runs-on"))
                    jobDef.RunsOn = jobObj["runs-on"]?.ToString() ?? "ubuntu-latest";

                // if:
                if (jobObj.ContainsKey("if"))
                    jobDef.If = jobObj["if"]?.ToString();

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

                // outputs:
                if (jobObj.ContainsKey("outputs") && jobObj["outputs"] is Dictionary<object, object> outputsObj)
                    jobDef.Outputs = outputsObj.ToDictionary(k => k.Key.ToString()!, v => v.Value?.ToString() ?? "");

                // strategy.matrix and strategy.fail-fast
                if (jobObj.ContainsKey("strategy") && jobObj["strategy"] is Dictionary<object, object> strategyObj)
                {
                    if (strategyObj.ContainsKey("fail-fast"))
                        jobDef.FailFast = strategyObj["fail-fast"]?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? true;

                    if (strategyObj.ContainsKey("max-parallel") && int.TryParse(strategyObj["max-parallel"]?.ToString(), out var maxP))
                        jobDef.MaxParallel = maxP;

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

                // environment: (string or object with name + url)
                if (jobObj.ContainsKey("environment"))
                {
                    var envVal = jobObj["environment"];
                    if (envVal is string envStr)
                    {
                        jobDef.Environment = envStr;
                    }
                    else if (envVal is Dictionary<object, object> envObj)
                    {
                        if (envObj.ContainsKey("name")) jobDef.Environment = envObj["name"]?.ToString();
                        if (envObj.ContainsKey("url")) jobDef.EnvironmentUrl = envObj["url"]?.ToString();
                    }
                }

                // Job-level uses: (reusable workflow call)
                if (jobObj.ContainsKey("uses"))
                    jobDef.Uses = jobObj["uses"]?.ToString();

                // Job-level with: (inputs for reusable workflow)
                if (jobObj.ContainsKey("with") && jobObj["with"] is Dictionary<object, object> jobWithDict)
                    jobDef.With = jobWithDict.ToDictionary(k => k.Key.ToString()!, v => v.Value?.ToString() ?? "");

                // Job-level secrets: (secrets for reusable workflow)
                if (jobObj.ContainsKey("secrets") && jobObj["secrets"] is Dictionary<object, object> jobSecretsDict)
                    jobDef.Secrets = jobSecretsDict.ToDictionary(k => k.Key.ToString()!, v => v.Value?.ToString() ?? "");

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
                            Uses = stepDict.ContainsKey("uses") ? stepDict["uses"]?.ToString() : null,
                            ContinueOnError = stepDict.ContainsKey("continue-on-error") &&
                                stepDict["continue-on-error"]?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true,
                            WorkingDirectory = stepDict.ContainsKey("working-directory")
                                ? stepDict["working-directory"]?.ToString() : null,
                            Shell = stepDict.ContainsKey("shell")
                                ? stepDict["shell"]?.ToString() : null
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

    /// <summary>
    /// Extracts the paths: and paths-ignore: filters from a push or pull_request trigger config.
    /// </summary>
    public static (List<string> paths, List<string> pathsIgnore) GetPathFilters(WorkflowDefinition workflow, string eventName)
    {
        var paths = new List<string>();
        var pathsIgnore = new List<string>();

        if (workflow.On is not Dictionary<object, object> onDict) return (paths, pathsIgnore);

        var eventKey = onDict.Keys.FirstOrDefault(k =>
            k.ToString()?.Equals(eventName, StringComparison.OrdinalIgnoreCase) == true);
        if (eventKey == null) return (paths, pathsIgnore);

        if (onDict[eventKey] is not Dictionary<object, object> eventConfig) return (paths, pathsIgnore);

        if (eventConfig.Keys.FirstOrDefault(k => k.ToString() == "paths") is { } pathsKey
            && eventConfig[pathsKey] is List<object> pathsList)
            paths = pathsList.Select(p => p?.ToString() ?? "").Where(p => !string.IsNullOrEmpty(p)).ToList();

        if (eventConfig.Keys.FirstOrDefault(k => k.ToString() == "paths-ignore") is { } ignoreKey
            && eventConfig[ignoreKey] is List<object> ignoreList)
            pathsIgnore = ignoreList.Select(p => p?.ToString() ?? "").Where(p => !string.IsNullOrEmpty(p)).ToList();

        return (paths, pathsIgnore);
    }

    /// <summary>
    /// Parses a composite action from .github/actions/{name}/action.yml in the repo.
    /// Returns the expanded steps.
    /// </summary>
    public List<StepDefinition>? ParseCompositeAction(string repoPath, string actionPath)
    {
        if (!Repository.IsValid(repoPath)) return null;

        try
        {
            using var repo = new Repository(repoPath);
            var head = repo.Head;
            if (head?.Tip == null) return null;

            var entry = head.Tip[actionPath];
            if (entry == null || entry.TargetType != TreeEntryTargetType.Blob) return null;

            var blob = (Blob)entry.Target;
            using var reader = new StreamReader(blob.GetContentStream());
            var yaml = reader.ReadToEnd();

            var raw = _deserializer.Deserialize<Dictionary<string, object?>>(yaml);
            if (raw == null) return null;

            // Check it's a composite action
            if (!raw.ContainsKey("runs") || raw["runs"] is not Dictionary<object, object> runsObj) return null;
            if (runsObj["using"]?.ToString() != "composite") return null;

            if (!runsObj.ContainsKey("steps") || runsObj["steps"] is not List<object> stepsList) return null;

            var steps = new List<StepDefinition>();
            foreach (var stepObj in stepsList)
            {
                if (stepObj is not Dictionary<object, object> stepDict) continue;
                steps.Add(new StepDefinition
                {
                    Id = stepDict.ContainsKey("id") ? stepDict["id"]?.ToString() : null,
                    Name = stepDict.ContainsKey("name") ? stepDict["name"]?.ToString() : null,
                    If = stepDict.ContainsKey("if") ? stepDict["if"]?.ToString() : null,
                    Run = stepDict.ContainsKey("run") ? stepDict["run"]?.ToString() : null,
                    Uses = stepDict.ContainsKey("uses") ? stepDict["uses"]?.ToString() : null,
                    Shell = stepDict.ContainsKey("shell") ? stepDict["shell"]?.ToString() : null,
                    WorkingDirectory = stepDict.ContainsKey("working-directory") ? stepDict["working-directory"]?.ToString() : null
                });
            }

            return steps;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Resolves a reusable workflow call by parsing the referenced workflow file.
    /// </summary>
    public WorkflowDefinition? ResolveReusableWorkflow(string repoPath, string workflowRef)
    {
        if (!Repository.IsValid(repoPath)) return null;

        // workflowRef is like "./.github/workflows/build.yml" or "owner/repo/.github/workflows/build.yml@main"
        // We only support local workflows (starting with ./)
        if (!workflowRef.StartsWith("./")) return null;

        var filePath = workflowRef.TrimStart('.', '/');

        try
        {
            using var repo = new Repository(repoPath);
            var head = repo.Head;
            if (head?.Tip == null) return null;

            var entry = head.Tip[filePath];
            if (entry == null || entry.TargetType != TreeEntryTargetType.Blob) return null;

            var blob = (Blob)entry.Target;
            using var reader = new StreamReader(blob.GetContentStream());
            var yaml = reader.ReadToEnd();

            return ParseYaml(yaml, Path.GetFileName(filePath));
        }
        catch
        {
            return null;
        }
    }

    private static void ParseWorkflowCallDefinition(WorkflowDefinition def)
    {
        if (def.On is not Dictionary<object, object> onDict) return;

        var callKey = onDict.Keys.FirstOrDefault(k =>
            k.ToString()?.Equals("workflow_call", StringComparison.OrdinalIgnoreCase) == true);
        if (callKey == null) return;

        def.IsReusable = true;
        def.WorkflowCall = new WorkflowCallDefinition();

        if (onDict[callKey] is not Dictionary<object, object> callObj) return;

        // Parse inputs
        if (callObj.Keys.FirstOrDefault(k => k.ToString() == "inputs") is { } inputsKey
            && callObj[inputsKey] is Dictionary<object, object> inputsDict)
        {
            foreach (var (inputName, inputValue) in inputsDict)
            {
                var input = new WorkflowInput { Name = inputName.ToString()! };
                if (inputValue is Dictionary<object, object> inputObj)
                {
                    if (inputObj.ContainsKey("description")) input.Description = inputObj["description"]?.ToString() ?? "";
                    if (inputObj.ContainsKey("required")) input.Required = inputObj["required"]?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
                    if (inputObj.ContainsKey("default")) input.Default = inputObj["default"]?.ToString();
                    if (inputObj.ContainsKey("type")) input.Type = inputObj["type"]?.ToString() ?? "string";
                }
                def.WorkflowCall.Inputs[input.Name] = input;
            }
        }

        // Parse outputs
        if (callObj.Keys.FirstOrDefault(k => k.ToString() == "outputs") is { } outputsKey
            && callObj[outputsKey] is Dictionary<object, object> outputsDict)
        {
            foreach (var (outName, outValue) in outputsDict)
            {
                var output = new WorkflowCallOutput();
                if (outValue is Dictionary<object, object> outObj)
                {
                    if (outObj.ContainsKey("description")) output.Description = outObj["description"]?.ToString() ?? "";
                    if (outObj.ContainsKey("value")) output.Value = outObj["value"]?.ToString() ?? "";
                }
                def.WorkflowCall.Outputs[outName.ToString()!] = output;
            }
        }

        // Parse secrets
        if (callObj.Keys.FirstOrDefault(k => k.ToString() == "secrets") is { } secretsKey
            && callObj[secretsKey] is Dictionary<object, object> secretsDict)
        {
            foreach (var (secName, secValue) in secretsDict)
            {
                var secret = new WorkflowCallSecret();
                if (secValue is Dictionary<object, object> secObj)
                {
                    if (secObj.ContainsKey("description")) secret.Description = secObj["description"]?.ToString() ?? "";
                    if (secObj.ContainsKey("required")) secret.Required = secObj["required"]?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
                }
                def.WorkflowCall.Secrets[secName.ToString()!] = secret;
            }
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
