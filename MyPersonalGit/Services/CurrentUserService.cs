using MyPersonalGit.Data;
using MyPersonalGit.Models;

namespace MyPersonalGit.Services;

/// <summary>
/// Scoped service (per Blazor circuit) that tracks who is currently logged in.
/// Pages inject this to get the current username instead of hardcoding it.
/// </summary>
public class CurrentUserService
{
    private readonly IAuthService _authService;
    private readonly ITwoFactorService _twoFactorService;
    private readonly ILdapAuthService _ldapAuthService;
    private User? _currentUser;
    private string? _sessionId;
    private bool _initialized;

    public CurrentUserService(IAuthService authService, ITwoFactorService twoFactorService, ILdapAuthService ldapAuthService)
    {
        _authService = authService;
        _twoFactorService = twoFactorService;
        _ldapAuthService = ldapAuthService;
    }

    public User? CurrentUser => _currentUser;
    public string Username => _currentUser?.Username ?? "anonymous";
    public string Email => _currentUser?.Email ?? "anonymous@local";
    public string DisplayName => _currentUser?.FullName ?? _currentUser?.Username ?? "anonymous";
    public bool IsLoggedIn => _currentUser != null;
    public bool IsAdmin => _currentUser?.IsAdmin ?? false;
    public string? SessionId => _sessionId;

    /// <summary>
    /// Initialize from a stored session ID (called from layouts/pages after reading browser storage).
    /// </summary>
    public async Task InitializeFromSessionAsync(string? sessionId)
    {
        if (_initialized && _sessionId == sessionId) return;
        _initialized = true;

        if (string.IsNullOrEmpty(sessionId))
        {
            _currentUser = null;
            _sessionId = null;
            return;
        }

        _sessionId = sessionId;
        _currentUser = await _authService.GetUserBySessionAsync(sessionId);

        // Session expired or invalid
        if (_currentUser == null)
        {
            _sessionId = null;
        }
    }

    /// <summary>
    /// Validates the user's password. Returns the user if valid, or null.
    /// If the user has 2FA enabled, the caller must then call CompleteTwoFactorLoginAsync.
    /// </summary>
    public async Task<(User? user, bool requires2FA)> ValidatePasswordAsync(string usernameOrEmail, string password)
    {
        // Try local auth first
        var user = await _authService.ValidatePasswordAsync(usernameOrEmail, password);

        // Fall back to LDAP/AD if local auth fails
        if (user == null)
        {
            user = await _ldapAuthService.AuthenticateAsync(usernameOrEmail, password);
            if (user != null)
            {
                // LDAP users skip 2FA (authenticated externally)
                return (user, false);
            }
        }

        if (user == null)
            return (null, false);

        var has2FA = await _twoFactorService.HasTwoFactorEnabled(user.Id);
        return (user, has2FA);
    }

    /// <summary>
    /// Completes login after 2FA verification. Returns the session ID to store in browser.
    /// </summary>
    public async Task<string?> CompleteTwoFactorLoginAsync(User user, string code, bool isRecoveryCode)
    {
        bool valid;
        if (isRecoveryCode)
            valid = await _twoFactorService.UseRecoveryCode(user.Id, code);
        else
            valid = await _twoFactorService.ValidateLogin(user.Id, code);

        if (!valid)
            return null;

        var session = await _authService.CreateSessionAsync(user);
        if (session == null) return null;

        _sessionId = session.SessionId;
        _currentUser = user;
        _initialized = true;
        return session.SessionId;
    }

    /// <summary>
    /// Called after successful login (no 2FA). Returns the session ID to store in browser.
    /// </summary>
    public async Task<string?> LoginAsync(string usernameOrEmail, string password)
    {
        var session = await _authService.LoginAsync(usernameOrEmail, password);
        if (session == null) return null;

        _sessionId = session.SessionId;
        _currentUser = await _authService.GetUserByUsernameAsync(session.Username);
        _initialized = true;
        return session.SessionId;
    }

    /// <summary>
    /// Creates a session for an already-validated user (no 2FA required).
    /// </summary>
    public async Task<string?> CreateSessionForUserAsync(User user)
    {
        var session = await _authService.CreateSessionAsync(user);
        if (session == null) return null;

        _sessionId = session.SessionId;
        _currentUser = user;
        _initialized = true;
        return session.SessionId;
    }

    /// <summary>
    /// Logout — clears current user and invalidates session.
    /// </summary>
    public async Task LogoutAsync()
    {
        if (_sessionId != null)
        {
            await _authService.LogoutAsync(_sessionId);
        }
        _currentUser = null;
        _sessionId = null;
        _initialized = false;
    }
}
