using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IBranchProtectionService
{
    Task<List<BranchProtectionRule>> GetRulesAsync(string repoName);
    Task<BranchProtectionRule> AddRuleAsync(string repoName, BranchProtectionRule rule);
    Task<BranchProtectionRule?> UpdateRuleAsync(string repoName, BranchProtectionRule updatedRule);
    Task<bool> DeleteRuleAsync(string repoName, int ruleId);
    Task<BranchProtectionRule?> GetMatchingRuleAsync(string repoName, string branchName);
    Task<bool> IsBranchProtectedAsync(string repoName, string branchName);
}

public class BranchProtectionService : IBranchProtectionService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<BranchProtectionService> _logger;

    public BranchProtectionService(IDbContextFactory<AppDbContext> dbFactory, ILogger<BranchProtectionService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<List<BranchProtectionRule>> GetRulesAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.BranchProtectionRules.Where(r => r.RepoName == repoName).ToListAsync();
    }

    public async Task<BranchProtectionRule> AddRuleAsync(string repoName, BranchProtectionRule rule)
    {
        using var db = _dbFactory.CreateDbContext();

        rule.RepoName = repoName;
        rule.CreatedAt = DateTime.UtcNow;
        rule.UpdatedAt = DateTime.UtcNow;

        db.BranchProtectionRules.Add(rule);
        await db.SaveChangesAsync();

        _logger.LogInformation("Branch protection rule added for pattern '{Pattern}' in {RepoName}", rule.BranchPattern, repoName);
        return rule;
    }

    public async Task<BranchProtectionRule?> UpdateRuleAsync(string repoName, BranchProtectionRule updatedRule)
    {
        using var db = _dbFactory.CreateDbContext();

        var existing = await db.BranchProtectionRules
            .FirstOrDefaultAsync(r => r.Id == updatedRule.Id && r.RepoName == repoName);

        if (existing == null)
            return null;

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

        await db.SaveChangesAsync();

        _logger.LogInformation("Branch protection rule {RuleId} updated in {RepoName}", updatedRule.Id, repoName);
        return existing;
    }

    public async Task<bool> DeleteRuleAsync(string repoName, int ruleId)
    {
        using var db = _dbFactory.CreateDbContext();

        var rule = await db.BranchProtectionRules
            .FirstOrDefaultAsync(r => r.Id == ruleId && r.RepoName == repoName);

        if (rule == null)
            return false;

        db.BranchProtectionRules.Remove(rule);
        await db.SaveChangesAsync();

        _logger.LogInformation("Branch protection rule {RuleId} deleted from {RepoName}", ruleId, repoName);
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
}
