namespace Identity.Application.Abstractions;

/// <summary>
/// Hashes and verifies OTP codes with a keyed HMAC — not a password KDF (bcrypt/Argon2/PBKDF2):
/// a 6-digit code bounded by a 5-minute TTL and a 5-attempt lockout doesn't need slow-hash
/// resistance, only "don't store the raw code" (see the ADR-0016 extension).
/// </summary>
public interface IOtpHasher
{
    /// <summary>Generates a new random salt for a challenge.</summary>
    /// <returns>A new random salt.</returns>
    string GenerateSalt();

    /// <summary>Computes the keyed-HMAC hash of a code, mixed with its per-challenge salt.</summary>
    /// <param name="code">The plaintext code.</param>
    /// <param name="salt">The per-challenge salt.</param>
    /// <returns>The computed hash.</returns>
    string Hash(string code, string salt);

    /// <summary>Constant-time-compares a submitted code's hash against the stored one.</summary>
    /// <param name="code">The submitted plaintext code.</param>
    /// <param name="salt">The challenge's salt.</param>
    /// <param name="expectedHash">The stored hash to compare against.</param>
    /// <returns><see langword="true"/> if the code matches.</returns>
    bool Verify(string code, string salt, string expectedHash);
}
