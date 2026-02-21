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
    private User? _currentUser;
    private string? _sessionId;
    private bool _initialized;

    public CurrentUserService(IAuthService authService)
    {
        _authService = authService;
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
    /// Called after successful login. Returns the session ID to store in browser.
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
