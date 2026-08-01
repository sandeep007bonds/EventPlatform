namespace Identity.Tests.Signing;

public sealed class JwtTokenIssuerTests
{
    [Fact]
    public async Task IssueAsync_MintsATokenWithTheExpectedClaimsAndKid()
    {
        using var rsa = RSA.Create(2048);
        var activeKey = new ActiveSigningKey("test-kid", rsa);

        var signingKeyProvider = Substitute.For<ISigningKeyProvider>();
        signingKeyProvider.GetActiveKeyAsync(Arg.Any<CancellationToken>()).Returns(activeKey);

        var issuer = new JwtTokenIssuer("https://identity.example", "eventplatform", TimeSpan.FromDays(7), signingKeyProvider);
        var buyerId = Guid.NewGuid();

        var issued = await issuer.IssueAsync(buyerId, CancellationToken.None);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(issued.AccessToken);

        token.Header.Kid.ShouldBe("test-kid");
        token.Header.Alg.ShouldBe(SecurityAlgorithms.RsaSha256);
        token.Issuer.ShouldBe("https://identity.example");
        token.Audiences.ShouldContain("eventplatform");
        token.Subject.ShouldBe(buyerId.ToString());
        token.Claims.ShouldContain(c => c.Type == "role" && c.Value == "buyer");

        // The whole point of ADR-0022: a buyer token must never carry a tenant claim.
        token.Claims.ShouldNotContain(c => c.Type == "tenant_id");
    }
}
