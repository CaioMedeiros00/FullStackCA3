namespace DriveNow.Models;

/// <summary>What we store in (encrypted) browser session storage for a logged-in user.</summary>
public class UserSession
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime LoginTimeUtc { get; set; }
}
