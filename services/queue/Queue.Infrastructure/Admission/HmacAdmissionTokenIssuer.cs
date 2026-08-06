namespace Queue.Infrastructure.Admission;

/// <summary>
/// Mints an HMAC-SHA256-signed admission token: <c>{eventId:N}.{sessionId:N}.{expUnixSeconds}.{signatureBase64}</c>,
/// where the signature covers the first three dot-joined fields. Verified locally by Inventory
/// against the same shared secret — see <c>HmacQueueAdmissionTokenValidator</c> in
/// <c>Inventory.Infrastructure</c>, which must stay byte-for-byte compatible with this format.
/// </summary>
/// <param name="key">The shared HMAC key bytes — the <c>QueueAdmission:HmacKey</c> config value.</param>
internal sealed class HmacAdmissionTokenIssuer(byte[] key) : IAdmissionTokenIssuer
{
    /// <inheritdoc />
    public string Issue(Guid eventId, Guid sessionId, TimeSpan validFor)
    {
        var expUnixSeconds = DateTimeOffset.UtcNow.Add(validFor).ToUnixTimeSeconds();
        var payload = $"{eventId:N}.{sessionId:N}.{expUnixSeconds}";

        using var hmac = new HMACSHA256(key);
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));

        return $"{payload}.{signature}";
    }
}
