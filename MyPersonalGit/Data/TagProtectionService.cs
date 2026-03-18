using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface ITagProtectionService
{
    Task<List<TagProtectionRule>> GetRulesAsync(string repoName);
    Task<TagProtectionRule> AddRuleAsync(string repoName, TagProtectionRule rule);
    Task<TagProtectionRule?> UpdateRuleAsync(string repoName, TagProtectionRule updatedRule);
    Task<bool> DeleteRuleAsync(string repoName, int ruleId);
    Task<TagProtectionRule?> GetMatchingRuleAsync(string repoName, string tagName);
    Task<bool> IsTagProtectedAsync(string repoName, string tagName);
}

public class TagProtectionService : ITagProtectionService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<TagProtectionService> _logger;

    public TagProtectionService(IDbContextFactory<AppDbContext> dbFactory, ILogger<TagProtectionService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<List<TagProtectionRule>> GetRulesAsync(string repoName)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.TagProtectionRules.Where(r => r.RepoName == repoName).ToListAsync();
    }

    public async Task<TagProtectionRule> AddRuleAsync(string repoName, TagProtectionRule rule)
    {
        using var db = _dbFactory.CreateDbContext();

        rule.RepoName = repoName;
        rule.CreatedAt = DateTime.UtcNow;
        rule.UpdatedAt = DateTime.UtcNow;

        db.TagProtectionRules.Add(rule);
        await db.SaveChangesAsync();

        _logger.LogInformation("Tag protection rule added for pattern '{Pattern}' in {RepoName}", rule.TagPattern, repoName);
        return rule;
    }

    public async Task<TagProtectionRule?> UpdateRuleAsync(string repoName, TagProtectionRule updatedRule)
    {
        using var db = _dbFactory.CreateDbContext();

        var existing = await db.TagProtectionRules
            .FirstOrDefaultAsync(r => r.Id == updatedRule.Id && r.RepoName == repoName);

        if (existing == null)
            return null;

        existing.TagPattern = updatedRule.TagPattern;
        existing.PreventDeletion = updatedRule.PreventDeletion;
        existing.PreventForcePush = updatedRule.PreventForcePush;
        existing.RestrictCreation = updatedRule.RestrictCreation;
        existing.AllowedUsers = updatedRule.AllowedUsers;
        existing.RequireSignedTags = updatedRule.RequireSignedTags;
        existing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        _logger.LogInformation("Tag protection rule {RuleId} updated in {RepoName}", updatedRule.Id, repoName);
        return existing;
    }

    public async Task<bool> DeleteRuleAsync(string repoName, int ruleId)
    {
        using var db = _dbFactory.CreateDbContext();

        var rule = await db.TagProtectionRules
            .FirstOrDefaultAsync(r => r.Id == ruleId && r.RepoName == repoName);

        if (rule == null)
            return false;

        db.TagProtectionRules.Remove(rule);
        await db.SaveChangesAsync();

        _logger.LogInformation("Tag protection rule {RuleId} deleted from {RepoName}", ruleId, repoName);
        return true;
    }

    public async Task<TagProtectionRule?> GetMatchingRuleAsync(string repoName, string tagName)
    {
        var rules = await GetRulesAsync(repoName);
        return rules.FirstOrDefault(r => TagMatchesPattern(tagName, r.TagPattern));
    }

    public async Task<bool> IsTagProtectedAsync(string repoName, string tagName)
    {
        var rule = await GetMatchingRuleAsync(repoName, tagName);
        return rule != null;
    }

    private static bool TagMatchesPattern(string tagName, string pattern)
    {
        if (pattern == tagName) return true;
        if (pattern.Contains('*'))
        {
            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(tagName, regexPattern);
        }
        return false;
    }
}
