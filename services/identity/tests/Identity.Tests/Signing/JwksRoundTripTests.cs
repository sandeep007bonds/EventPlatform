namespace Identity.Tests.Signing;

/// <summary>
/// The strongest achievable proof without a live HTTP discovery fetch: build the exact JWKS DTO
/// <c>DiscoveryEndpoints</c> serves, serialize it through the same JSON options the API host uses
/// (camelCase policy), parse it back with <see cref="Microsoft.IdentityModel.Tokens.JsonWebKeySet"/>
/// (the same type ASP.NET Core's own OIDC discovery machinery uses), and validate a token minted
/// by <see cref="JwtTokenIssuer"/> against the round-tripped key. If either the JSON shape or the
/// signing chain were wrong, this fails.
/// </summary>
public sealed class JwksRoundTripTests
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public async Task RoundTrippedJwks_ValidatesATokenMintedByTheSameKey()
    {
        using var rsa = RSA.Create(2048);
        var activeKey = new ActiveSigningKey("test-kid", rsa);
        var publicParameters = rsa.ExportParameters(includePrivateParameters: false);

        var jwks = new JsonWebKeySetDto(
        [
            new JsonWebKeyDto(
                KeyType: "RSA",
                Use: "sig",
                KeyId: activeKey.Kid,
                Algorithm: "RS256",
                Modulus: Base64UrlEncoder.Encode(publicParameters.Modulus),
                Exponent: Base64UrlEncoder.Encode(publicParameters.Exponent)),
        ]);

        var json = JsonSerializer.Serialize(jwks, CamelCaseOptions);

        // The explicit [JsonPropertyName] attributes must win over the camelCase policy — this is
        // the direct regression test for that risk.
        json.ShouldContain("\"kty\"");
        json.ShouldContain("\"kid\"");
        json.ShouldContain("\"n\"");
        json.ShouldContain("\"e\"");
        json.ShouldNotContain("\"keyType\"");
        json.ShouldNotContain("\"keyId\"");

        var parsedJwks = new JsonWebKeySet(json);
        var parsedKey = parsedJwks.Keys.ShouldHaveSingleItem();
        parsedKey.Kid.ShouldBe(activeKey.Kid);

        var signingKeyProvider = Substitute.For<ISigningKeyProvider>();
        signingKeyProvider.GetActiveKeyAsync(Arg.Any<CancellationToken>()).Returns(activeKey);
        var issuer = new JwtTokenIssuer("https://identity.example", "eventplatform", TimeSpan.FromDays(7), signingKeyProvider);
        var issued = await issuer.IssueAsync(Guid.NewGuid(), "buyer", null, CancellationToken.None);

        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = "https://identity.example",
            ValidAudience = "eventplatform",
            IssuerSigningKey = parsedKey,
        };

        // Throws if invalid — the assertion IS that this does not throw.
        handler.ValidateToken(issued.AccessToken, validationParameters, out _);
    }

    [Fact]
    public void OidcDiscoveryDocument_SerializesWithSpecFieldNames_NotCamelCase()
    {
        var document = new OidcDiscoveryDocument(
            Issuer: "https://identity.example",
            JwksUri: "https://identity.example/.well-known/jwks.json",
            TokenEndpoint: "https://identity.example/v1/otp/verify",
            ResponseTypesSupported: ["token"],
            SubjectTypesSupported: ["public"],
            IdTokenSigningAlgValuesSupported: ["RS256"]);

        var json = JsonSerializer.Serialize(document, CamelCaseOptions);

        json.ShouldContain("\"response_types_supported\"");
        json.ShouldContain("\"subject_types_supported\"");
        json.ShouldContain("\"id_token_signing_alg_values_supported\"");
        json.ShouldContain("\"jwks_uri\"");
        json.ShouldContain("\"token_endpoint\"");
        json.ShouldNotContain("\"responseTypesSupported\"");
    }
}
