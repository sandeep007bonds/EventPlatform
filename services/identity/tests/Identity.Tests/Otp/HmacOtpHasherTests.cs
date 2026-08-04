namespace Identity.Tests.Otp;

public sealed class HmacOtpHasherTests
{
    private readonly HmacOtpHasher hasher = new(Encoding.UTF8.GetBytes("test-hmac-key"));

    [Fact]
    public void Verify_WithCorrectCodeAndSalt_ReturnsTrue()
    {
        var salt = hasher.GenerateSalt();
        var hash = hasher.Hash("123456", salt);

        hasher.Verify("123456", salt, hash).ShouldBeTrue();
    }

    [Fact]
    public void Verify_WithWrongCode_ReturnsFalse()
    {
        var salt = hasher.GenerateSalt();
        var hash = hasher.Hash("123456", salt);

        hasher.Verify("654321", salt, hash).ShouldBeFalse();
    }

    [Fact]
    public void Hash_SameCodeDifferentSalts_ProducesDifferentHashes()
    {
        var saltA = hasher.GenerateSalt();
        var saltB = hasher.GenerateSalt();

        var hashA = hasher.Hash("123456", saltA);
        var hashB = hasher.Hash("123456", saltB);

        hashA.ShouldNotBe(hashB);
    }
}
