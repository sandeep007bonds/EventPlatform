namespace Identity.Infrastructure.Otp;

/// <summary>
/// Keyed-HMAC-SHA256 OTP hashing. The key comes from configuration — a service-owned secret,
/// never persisted to the <c>identity</c> database, distinct from the RSA signing key. See the
/// ADR-0016 extension for why HMAC (not bcrypt/Argon2) is the right primitive for a 6-digit,
/// 5-minute-TTL, 5-attempt-locked code.
/// </summary>
/// <param name="key">The HMAC key bytes.</param>
internal sealed class HmacOtpHasher(byte[] key) : IOtpHasher
{
    /// <inheritdoc />
    public string GenerateSalt() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

    /// <inheritdoc />
    public string Hash(string code, string salt)
    {
        using var hmac = new HMACSHA256(key);
        var input = Encoding.UTF8.GetBytes(salt + code);
        return Convert.ToBase64String(hmac.ComputeHash(input));
    }

    /// <inheritdoc />
    public bool Verify(string code, string salt, string expectedHash)
    {
        var actual = Hash(code, salt);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(actual),
            Convert.FromBase64String(expectedHash));
    }
}
