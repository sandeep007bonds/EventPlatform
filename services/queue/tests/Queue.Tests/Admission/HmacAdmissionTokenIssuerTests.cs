namespace Queue.Tests.Admission;

public sealed class HmacAdmissionTokenIssuerTests
{
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("test-queue-admission-hmac-key");
    private readonly HmacAdmissionTokenIssuer issuer = new(Key);

    [Fact]
    public void Issue_ThenVerify_WithSameKey_Succeeds()
    {
        var eventId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();

        var token = issuer.Issue(eventId, sessionId, TimeSpan.FromMinutes(10));

        Verify(token, eventId, Key, DateTimeOffset.UtcNow).ShouldBeTrue();
    }

    [Fact]
    public void Verify_ForADifferentEvent_Fails()
    {
        var token = issuer.Issue(Guid.CreateVersion7(), Guid.CreateVersion7(), TimeSpan.FromMinutes(10));

        Verify(token, Guid.CreateVersion7(), Key, DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void Verify_AfterExpiry_Fails()
    {
        var eventId = Guid.CreateVersion7();
        var token = issuer.Issue(eventId, Guid.CreateVersion7(), TimeSpan.FromSeconds(1));

        Verify(token, eventId, Key, DateTimeOffset.UtcNow.AddMinutes(1)).ShouldBeFalse();
    }

    [Fact]
    public void Verify_WithATamperedSignature_Fails()
    {
        var eventId = Guid.CreateVersion7();
        var token = issuer.Issue(eventId, Guid.CreateVersion7(), TimeSpan.FromMinutes(10));
        var tampered = string.Concat(token.AsSpan(0, token.Length - 4), "XXXX");

        Verify(tampered, eventId, Key, DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void Verify_WithAWrongKey_Fails()
    {
        var eventId = Guid.CreateVersion7();
        var token = issuer.Issue(eventId, Guid.CreateVersion7(), TimeSpan.FromMinutes(10));

        Verify(token, eventId, Encoding.UTF8.GetBytes("a-completely-different-key"), DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    // Mirrors HmacQueueAdmissionTokenValidator (Inventory.Infrastructure) byte-for-byte, without a
    // cross-service project reference — proves the issuer's output is verifiable by recomputing
    // the same signature, exactly what the real validator does independently in Inventory.
    private static bool Verify(string token, Guid eventId, byte[] key, DateTimeOffset now)
    {
        var parts = token.Split('.');
        if (parts.Length != 4)
        {
            return false;
        }

        if (!Guid.TryParse(parts[0], out var tokenEventId) || tokenEventId != eventId)
        {
            return false;
        }

        if (!long.TryParse(parts[2], out var expUnixSeconds) || DateTimeOffset.FromUnixTimeSeconds(expUnixSeconds) < now)
        {
            return false;
        }

        var payload = $"{parts[0]}.{parts[1]}.{parts[2]}";
        using var hmac = new HMACSHA256(key);
        var expected = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        return expected == parts[3];
    }
}
