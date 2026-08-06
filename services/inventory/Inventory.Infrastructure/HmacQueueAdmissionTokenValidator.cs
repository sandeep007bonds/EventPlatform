namespace Inventory.Infrastructure;

/// <summary>
/// Verifies a Queue-service admission token — format <c>{eventId:N}.{sessionId:N}.{expUnixSeconds}.{signatureBase64}</c>
/// — by recomputing the HMAC-SHA256 signature against the same shared secret Queue signed with.
/// Must stay byte-for-byte compatible with <c>HmacAdmissionTokenIssuer</c> in
/// <c>Queue.Infrastructure</c>. No network call to Queue (ADR-0026).
/// </summary>
/// <param name="key">The shared HMAC key bytes — the <c>QueueAdmission:HmacKey</c> config value.</param>
internal sealed class HmacQueueAdmissionTokenValidator(byte[] key) : IQueueAdmissionTokenValidator
{
    /// <inheritdoc />
    public bool IsValid(string? token, Guid eventId, DateTimeOffset now)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        var parts = token.Split('.');
        if (parts.Length != 4)
        {
            return false;
        }

        var (tokenEventId, sessionId, exp, signature) = (parts[0], parts[1], parts[2], parts[3]);
        if (!Guid.TryParse(tokenEventId, out var parsedEventId) || parsedEventId != eventId)
        {
            return false;
        }

        if (!long.TryParse(exp, out var expUnixSeconds) || DateTimeOffset.FromUnixTimeSeconds(expUnixSeconds) < now)
        {
            return false;
        }

        var payload = $"{tokenEventId}.{sessionId}.{exp}";
        using var hmac = new HMACSHA256(key);
        var expectedSignature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));

        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(expectedSignature),
            TryDecodeBase64(signature));
    }

    // A tampered/malformed signature segment must fail comparison, not throw — an empty byte array
    // never matches a real HMAC digest.
    private static byte[] TryDecodeBase64(string value)
    {
        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            return Array.Empty<byte>();
        }
    }
}
