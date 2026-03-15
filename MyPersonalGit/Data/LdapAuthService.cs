using System.DirectoryServices.Protocols;
using System.Net;
using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface ILdapAuthService
{
    /// <summary>
    /// Authenticate a user against LDAP/AD. Returns a User object if successful.
    /// If the user doesn't exist locally, creates a new local account from LDAP attributes.
    /// </summary>
    Task<User?> AuthenticateAsync(string username, string password);

    /// <summary>
    /// Test the LDAP connection with current settings. Returns (success, message).
    /// </summary>
    Task<(bool Success, string Message)> TestConnectionAsync();
}

/// <summary>
/// LDAP / Active Directory authentication service.
///
/// Flow:
///   1. Bind to LDAP with the configured service account (or anonymous)
///   2. Search for the user by the configured filter (e.g., sAMAccountName={0} for AD)
///   3. Attempt to bind as the found user DN with their password
///   4. On success, sync user attributes (email, display name) to the local database
///   5. Optionally check group membership for admin privileges
///
/// Supports:
///   - Active Directory (LDAP/LDAPS)
///   - OpenLDAP
///   - StartTLS
///   - Simple bind and search+bind authentication
/// </summary>
public class LdapAuthService : ILdapAuthService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAdminService _adminService;
    private readonly ILogger<LdapAuthService> _logger;

    public LdapAuthService(IDbContextFactory<AppDbContext> dbFactory, IAdminService adminService, ILogger<LdapAuthService> logger)
    {
        _dbFactory = dbFactory;
        _adminService = adminService;
        _logger = logger;
    }

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        var settings = await _adminService.GetSystemSettingsAsync();
        if (!settings.LdapEnabled || string.IsNullOrWhiteSpace(settings.LdapServer))
            return null;

        try
        {
            // Step 1: Connect and bind with service account to search for the user
            string userDn;
            SearchResultEntry entry;
            using (var conn = CreateConnection(settings))
            {
                if (!string.IsNullOrWhiteSpace(settings.LdapBindDn))
                    conn.Bind(new NetworkCredential(settings.LdapBindDn, settings.LdapBindPassword));
                else
                    conn.Bind(); // anonymous bind

                // Step 2: Search for the user
                var filter = string.Format(settings.LdapUserFilter, EscapeLdapFilter(username));
                var searchRequest = new SearchRequest(
                    settings.LdapSearchBase,
                    filter,
                    SearchScope.Subtree,
                    settings.LdapUsernameAttribute,
                    settings.LdapEmailAttribute,
                    settings.LdapDisplayNameAttribute,
                    "memberOf" // for group membership checks
                );

                var searchResponse = (SearchResponse)conn.SendRequest(searchRequest);
                if (searchResponse.Entries.Count == 0)
                {
                    _logger.LogDebug("LDAP user not found: {Username}", username);
                    return null;
                }

                entry = searchResponse.Entries[0];
                userDn = entry.DistinguishedName;
            }

            // Step 3: Bind as the user to verify password
            using (var userConn = CreateConnection(settings))
            {
                try
                {
                    userConn.Bind(new NetworkCredential(userDn, password));
                }
                catch (LdapException ex)
                {
                    _logger.LogDebug("LDAP bind failed for {UserDn}: {Error}", userDn, ex.Message);
                    return null;
                }
            }

            // Step 4: Extract attributes
            var ldapUsername = GetAttribute(entry, settings.LdapUsernameAttribute) ?? username;
            var email = GetAttribute(entry, settings.LdapEmailAttribute) ?? $"{ldapUsername}@ldap.local";
            var displayName = GetAttribute(entry, settings.LdapDisplayNameAttribute);

            // Step 5: Check admin group membership
            var isAdmin = false;
            if (!string.IsNullOrWhiteSpace(settings.LdapAdminGroupDn))
            {
                var memberOf = GetAttributes(entry, "memberOf");
                isAdmin = memberOf.Any(g =>
                    g.Equals(settings.LdapAdminGroupDn, StringComparison.OrdinalIgnoreCase));
            }

            // Step 6: Sync to local database
            return await SyncLocalUser(ldapUsername, email, displayName, isAdmin);
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "LDAP authentication error for {Username}", username);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during LDAP authentication for {Username}", username);
            return null;
        }
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync()
    {
        var settings = await _adminService.GetSystemSettingsAsync();
        if (!settings.LdapEnabled || string.IsNullOrWhiteSpace(settings.LdapServer))
            return (false, "LDAP is not enabled or server is not configured.");

        try
        {
            using var conn = CreateConnection(settings);

            if (!string.IsNullOrWhiteSpace(settings.LdapBindDn))
                conn.Bind(new NetworkCredential(settings.LdapBindDn, settings.LdapBindPassword));
            else
                conn.Bind();

            // Try a simple search to validate the search base
            var searchRequest = new SearchRequest(
                settings.LdapSearchBase,
                "(objectClass=*)",
                SearchScope.Base);

            var response = (SearchResponse)conn.SendRequest(searchRequest);

            return (true, $"Connection successful. Search base '{settings.LdapSearchBase}' is accessible.");
        }
        catch (LdapException ex)
        {
            return (false, $"LDAP error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Connection failed: {ex.Message}");
        }
    }

    private LdapConnection CreateConnection(SystemSettings settings)
    {
        var server = new LdapDirectoryIdentifier(settings.LdapServer, settings.LdapPort);
        var conn = new LdapConnection(server)
        {
            AuthType = AuthType.Basic,
            AutoBind = false
        };

        conn.SessionOptions.ProtocolVersion = 3;
        conn.SessionOptions.ReferralChasing = ReferralChasingOptions.None;

        if (settings.LdapUseSsl)
        {
            conn.SessionOptions.SecureSocketLayer = true;
        }
        else if (settings.LdapStartTls)
        {
            conn.SessionOptions.StartTransportLayerSecurity(null);
        }

        return conn;
    }

    private async Task<User> SyncLocalUser(string username, string email, string? displayName, bool isAdmin)
    {
        using var db = _dbFactory.CreateDbContext();

        var existingUser = await db.Users.FirstOrDefaultAsync(u =>
            u.Username.ToLower() == username.ToLower());

        if (existingUser != null)
        {
            // Update attributes from LDAP
            existingUser.Email = email;
            if (!string.IsNullOrEmpty(displayName))
                existingUser.FullName = displayName;
            existingUser.LastLoginAt = DateTime.UtcNow;
            existingUser.IsActive = true;

            // Only promote to admin from LDAP group, never demote
            // (allows local admin override)
            if (isAdmin)
                existingUser.IsAdmin = true;

            await db.SaveChangesAsync();
            _logger.LogInformation("LDAP user {Username} synced from directory", username);
            return existingUser;
        }

        // Create new local user from LDAP
        var newUser = new User
        {
            Username = username,
            Email = email,
            FullName = displayName,
            // Random password hash — LDAP users auth via LDAP, not local password
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), workFactor: 4),
            IsAdmin = isAdmin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        db.Users.Add(newUser);
        await db.SaveChangesAsync();

        _logger.LogInformation("Created local account for LDAP user {Username} ({Email})", username, email);
        return newUser;
    }

    private static string? GetAttribute(SearchResultEntry entry, string attrName)
    {
        if (string.IsNullOrEmpty(attrName)) return null;
        var attr = entry.Attributes[attrName];
        if (attr == null || attr.Count == 0) return null;
        return attr[0]?.ToString();
    }

    private static List<string> GetAttributes(SearchResultEntry entry, string attrName)
    {
        var result = new List<string>();
        var attr = entry.Attributes[attrName];
        if (attr == null) return result;
        for (int i = 0; i < attr.Count; i++)
        {
            var val = attr[i]?.ToString();
            if (val != null) result.Add(val);
        }
        return result;
    }

    private static string EscapeLdapFilter(string input)
    {
        // RFC 4515 escape special characters in LDAP search filters
        return input
            .Replace("\\", "\\5c")
            .Replace("*", "\\2a")
            .Replace("(", "\\28")
            .Replace(")", "\\29")
            .Replace("\0", "\\00");
    }
}
