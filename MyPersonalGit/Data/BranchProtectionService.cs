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
    void InstallPreReceiveHook(string repoPath, string repoName);
}

public class BranchProtectionService : IBranchProtectionService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<BranchProtectionService> _logger;
    private readonly IConfiguration _config;

    public BranchProtectionService(IDbContextFactory<AppDbContext> dbFactory, ILogger<BranchProtectionService> logger, IConfiguration config)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _config = config;
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

        // Install pre-receive hook for push enforcement
        var projectRoot = _config["Git:ProjectRoot"] ?? "/repos";
        var repoPath = Path.Combine(projectRoot, repoName);
        if (!Directory.Exists(repoPath))
            repoPath = repoPath + ".git";
        if (Directory.Exists(repoPath))
            InstallPreReceiveHook(repoPath, repoName);

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
        existing.RequireCodeOwnersApproval = updatedRule.RequireCodeOwnersApproval;
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

    public void InstallPreReceiveHook(string repoPath, string repoName)
    {
        try
        {
            var hooksDir = Path.Combine(repoPath, "hooks");
            Directory.CreateDirectory(hooksDir);

            var hookPath = Path.Combine(hooksDir, "pre-receive");
            var hookScript = $@"#!/bin/bash
# Auto-installed by MyPersonalGit for branch protection enforcement
REPO_NAME=""{repoName}""
APP_URL=""http://localhost:8080""
PUSH_USER=""${{REMOTE_USER:-unknown}}""

UPDATES=""[]""
while read oldrev newrev refname; do
    # Detect force push (non-fast-forward)
    IS_FORCE=""false""
    if [ ""$oldrev"" != ""0000000000000000000000000000000000000000"" ] && [ ""$newrev"" != ""0000000000000000000000000000000000000000"" ]; then
        if ! git merge-base --is-ancestor ""$oldrev"" ""$newrev"" 2>/dev/null; then
            IS_FORCE=""true""
        fi
    fi
    UPDATES=$(echo ""$UPDATES"" | sed ""s/]$/,\{{\\""oldSha\\"":\\""$oldrev\\"",\\""newSha\\"":\\""$newrev\\"",\\""refName\\"":\\""$refname\\"",\\""isForcePush\\"":$IS_FORCE\}}]/"")
done

# Fix leading comma
UPDATES=$(echo ""$UPDATES"" | sed 's/\[,/[/')

PAYLOAD=""\{{\\""repoName\\"":\\""$REPO_NAME\\"",\\""pushUser\\"":\\""$PUSH_USER\\"",\\""updates\\"":$UPDATES\}}""

RESPONSE=$(curl -s -X POST ""$APP_URL/api/v1/hooks/pre-receive"" \
    -H ""Content-Type: application/json"" \
    -d ""$PAYLOAD"" 2>/dev/null)

if [ $? -ne 0 ]; then
    # If the API is unreachable, allow the push (fail open)
    exit 0
fi

ALLOWED=$(echo ""$RESPONSE"" | grep -o '""allowed"":true')
if [ -z ""$ALLOWED"" ]; then
    MESSAGE=$(echo ""$RESPONSE"" | grep -o '""message"":""[^""]*""' | sed 's/""message"":""//' | sed 's/""$//')
    echo ""*** $MESSAGE"" >&2
    exit 1
fi

exit 0
";
            File.WriteAllText(hookPath, hookScript);

            // Make executable on Unix
            if (!OperatingSystem.IsWindows())
            {
                System.Diagnostics.Process.Start("chmod", $"+x \"{hookPath}\"")?.WaitForExit(5000);
            }

            _logger.LogInformation("Pre-receive hook installed for {RepoName} at {HookPath}", repoName, hookPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to install pre-receive hook for {RepoName}", repoName);
        }
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
