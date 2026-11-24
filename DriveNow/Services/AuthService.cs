using System.Security.Cryptography;
using System.Text;
using DriveNow.Auth;
using DriveNow.Models;

namespace DriveNow.Services;

/// <summary>
/// Validates credentials against hardcoded staff accounts and signs users in/out
/// through the custom AuthenticationStateProvider.
/// Passwords are stored as SHA-256 hashes rather than plain text.
/// (A production system would use ASP.NET Identity - not required for this project.)
/// </summary>
public class AuthService(DriveNowAuthStateProvider authStateProvider, ILogger<AuthService> logger)
{
    public const string AdminRole = "Admin";
    public const string StaffRole = "Staff";

    private sealed record StaffAccount(string Username, string PasswordHash, string DisplayName, string Role);

    private static readonly StaffAccount[] Accounts =
    [
        // admin / Admin123!
        new("admin", "3EB3FE66B31E3B4D10FA70B5CAD49C7112294AF6AE4E476A1C405155D45AA121", "Administrator", AdminRole),
        // staff / Staff123!
        new("staff", "05DD4A1376A72D9A5E0FAD32000F7E61651A5CEF5C9C9A0C3816C7443DAFBF6F", "Front Desk Staff", StaffRole)
    ];

    public async Task<ServiceResult> LoginAsync(LoginModel model)
    {
        var account = Accounts.FirstOrDefault(a =>
            string.Equals(a.Username, model.Username.Trim(), StringComparison.OrdinalIgnoreCase));

        if (account is null || !PasswordMatches(model.Password, account.PasswordHash))
        {
            logger.LogInformation("Failed login attempt for {Username}", model.Username);
            return ServiceResult.Fail("Invalid username or password.");
        }

        await authStateProvider.SignInAsync(new UserSession
        {
            Username = account.Username,
            DisplayName = account.DisplayName,
            Role = account.Role,
            LoginTimeUtc = DateTime.UtcNow
        });

        logger.LogInformation("{Username} logged in", account.Username);
        return ServiceResult.Ok();
    }

    public Task LogoutAsync() => authStateProvider.SignOutAsync();

    private static bool PasswordMatches(string password, string expectedHash)
    {
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        var expected = Convert.FromHexString(expectedHash);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
