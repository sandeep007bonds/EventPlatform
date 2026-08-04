namespace Identity.Api.Endpoints;

/// <summary>
/// Maps the OIDC discovery document and JWKS endpoints. Neither is gateway-routed — both are
/// fetched server-to-server by the 5 existing services' own Authority-based discovery
/// (<c>AuthenticationExtensions.cs</c>), never by a browser.
/// </summary>
public static class DiscoveryEndpoints
{
    /// <summary>Maps the discovery endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapDiscoveryEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // OIDC spec requires these at exactly {issuer}/.well-known/... — root, not under /v1.
        app.MapGet("/.well-known/openid-configuration", GetDiscoveryDocument).WithName("OidcDiscovery").AllowAnonymous();
        app.MapGet("/.well-known/jwks.json", GetJwksAsync).WithName("Jwks").AllowAnonymous();

        return app;
    }

    private static IResult GetDiscoveryDocument(IConfiguration configuration)
    {
        var issuer = configuration["Identity:Jwt:Issuer"]!;
        return Results.Ok(new OidcDiscoveryDocument(
            Issuer: issuer,
            JwksUri: $"{issuer}/.well-known/jwks.json",
            TokenEndpoint: $"{issuer}/v1/otp/verify",
            ResponseTypesSupported: ["token"],
            SubjectTypesSupported: ["public"],
            IdTokenSigningAlgValuesSupported: ["RS256"]));
    }

    private static async Task<IResult> GetJwksAsync(ISigningKeyProvider signingKeyProvider, CancellationToken cancellationToken)
    {
        var keys = await signingKeyProvider.GetPublicKeysAsync(cancellationToken);
        var jwks = new JsonWebKeySetDto(keys.Select(k => new JsonWebKeyDto(
            KeyType: "RSA",
            Use: "sig",
            KeyId: k.Kid,
            Algorithm: "RS256",
            Modulus: Base64UrlEncoder.Encode(k.PublicParameters.Modulus),
            Exponent: Base64UrlEncoder.Encode(k.PublicParameters.Exponent))).ToList());
        return Results.Ok(jwks);
    }
}
