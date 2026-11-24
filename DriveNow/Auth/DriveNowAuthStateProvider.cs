using System.Security.Claims;
using DriveNow.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DriveNow.Auth;

/// <summary>
/// Custom AuthenticationStateProvider. The logged-in user is kept in memory for the
/// circuit and persisted in encrypted browser session storage, so a page refresh keeps
/// the user logged in until the tab is closed or the session expires.
/// </summary>
public class DriveNowAuthStateProvider(ProtectedSessionStorage sessionStorage, ILogger<DriveNowAuthStateProvider> logger)
    : AuthenticationStateProvider
{
    private const string StorageKey = "drivenow.session";
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);

    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));
    private UserSession? _session;
    private bool _restored;

    public UserSession? CurrentSession => _session;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_restored)
        {
            _restored = true;
            try
            {
                var stored = await sessionStorage.GetAsync<UserSession>(StorageKey);
                _session = stored.Success ? stored.Value : null;
            }
            catch (Exception ex)
            {
                // Storage can be unavailable (e.g. tampered value, JS not ready) - treat as logged out.
                logger.LogWarning(ex, "Could not restore user session from storage");
                _session = null;
            }
        }

        if (_session is null)
        {
            return Anonymous;
        }

        if (DateTime.UtcNow - _session.LoginTimeUtc > SessionLifetime)
        {
            await SignOutAsync();
            return Anonymous;
        }

        return new AuthenticationState(CreatePrincipal(_session));
    }

    public async Task SignInAsync(UserSession session)
    {
        _session = session;
        _restored = true;
        await sessionStorage.SetAsync(StorageKey, session);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(CreatePrincipal(session))));
    }

    public async Task SignOutAsync()
    {
        _session = null;
        _restored = true;
        try
        {
            await sessionStorage.DeleteAsync(StorageKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not clear user session from storage");
        }
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private static ClaimsPrincipal CreatePrincipal(UserSession session)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, session.Username),
            new Claim(ClaimTypes.GivenName, session.DisplayName),
            new Claim(ClaimTypes.Role, session.Role)
        ], authenticationType: "DriveNowAuth");

        return new ClaimsPrincipal(identity);
    }
}
