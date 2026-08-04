namespace Identity.Application.Abstractions;

/// <summary>
/// Hashes and verifies organizer passwords. Implemented in Infrastructure as a thin adapter over
/// <c>Microsoft.Extensions.Identity.Core</c>'s <c>PasswordHasher&lt;TUser&gt;</c> — a vetted
/// primitive, unlike OTP's deliberately hand-rolled keyed-HMAC hashing (a password's risk profile
/// differs from a short-TTL/attempt-capped numeric code; see ADR-0023).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a plaintext password for storage.</summary>
    /// <param name="password">The plaintext password.</param>
    /// <returns>The hash to persist as <see cref="OrganizerAccount.PasswordHash"/>.</returns>
    string Hash(string password);

    /// <summary>Verifies a plaintext password against an account's current stored hash.</summary>
    /// <param name="account">The account to verify against.</param>
    /// <param name="password">The submitted plaintext password.</param>
    /// <returns><see langword="true"/> if the password matches.</returns>
    bool Verify(OrganizerAccount account, string password);
}
