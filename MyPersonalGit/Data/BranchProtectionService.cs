using System.Text.Json;
using System.Text.RegularExpressions;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public class BranchProtectionService
{
    private readonly string _dataPath;
    private readonly ILogger<BranchProtectionService> _logger;

    public BranchProtectionService(IConfiguration config, ILogger<BranchProtectionService> logger)
    {
        var projectRoot = config["Git:ProjectRoot"] ?? "/repos";
        _dataPath = Path.Combine(projectRoot, ".data");
        _logger = logger;
        Directory.CreateDirectory(_dataPath);
    }

    private string GetFilePath(string repoName) =>
        Path.Combine(_dataPath, $"{repoName}_branch_protection.json");

    public async Task<List<BranchProtectionRule>> GetRulesAsync(string repoName)
    {
        var filePath = GetFilePath(repoName);
        if (!File.Exists(filePath))
            return new List<BranchProtectionRule>();

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<BranchProtectionRule>>(json) ?? new List<BranchProtectionRule>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load branch protection rules for {RepoName}", repoName);
            return new List<BranchProtectionRule>();
        }
    }

    public async Task<BranchProtectionRule> AddRuleAsync(string repoName, BranchProtectionRule rule)
    {
        var rules = await GetRulesAsync(repoName);
        rule.Id = rules.Count > 0 ? rules.Max(r => r.Id) + 1 : 1;
        rule.CreatedAt = DateTime.UtcNow;
        rule.UpdatedAt = DateTime.UtcNow;
        rules.Add(rule);
        await SaveRulesAsync(repoName, rules);
        return rule;
    }

    public async Task<BranchProtectionRule?> UpdateRuleAsync(string repoName, BranchProtectionRule updatedRule)
    {
        var rules = await GetRulesAsync(repoName);
        var existing = rules.FirstOrDefault(r => r.Id == updatedRule.Id);
        if (existing == null) return null;

        existing.BranchPattern = updatedRule.BranchPattern;
        existing.RequirePullRequest = updatedRule.RequirePullRequest;
        existing.RequiredApprovals = updatedRule.RequiredApprovals;
        existing.RequireStatusChecks = updatedRule.RequireStatusChecks;
        existing.RequiredStatusChecks = updatedRule.RequiredStatusChecks;
        existing.PreventForcePush = updatedRule.PreventForcePush;
        existing.PreventDeletion = updatedRule.PreventDeletion;
        existing.RequireLinearHistory = updatedRule.RequireLinearHistory;
        existing.RestrictPushes = updatedRule.RestrictPushes;
        existing.AllowedPushUsers = updatedRule.AllowedPushUsers;
        existing.UpdatedAt = DateTime.UtcNow;

        await SaveRulesAsync(repoName, rules);
        return existing;
    }

    public async Task<bool> DeleteRuleAsync(string repoName, int ruleId)
    {
        var rules = await GetRulesAsync(repoName);
        var rule = rules.FirstOrDefault(r => r.Id == ruleId);
        if (rule == null) return false;

        rules.Remove(rule);
        await SaveRulesAsync(repoName, rules);
        return true;
    }

    public async Task<BranchProtectionRule?> GetMatchingRuleAsync(string repoName, string branchName)
    {
        var rules = await GetRulesAsync(repoName);
        return rules.FirstOrDefault(r => BranchMatchesPattern(branchName, r.BranchPattern));
    }

    public async Task<bool> IsBranchProtectedAsync(string repoName, string branchName)
    {
        var rule = await GetMatchingRuleAsync(repoName, branchName);
        return rule != null;
    }

    private static bool BranchMatchesPattern(string branchName, string pattern)
    {
        if (pattern == branchName) return true;
        if (pattern.Contains('*'))
        {
            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(branchName, regexPattern);
        }
        return false;
    }

    private async Task SaveRulesAsync(string repoName, List<BranchProtectionRule> rules)
    {
        var filePath = GetFilePath(repoName);
        var json = JsonSerializer.Serialize(rules, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }
}
