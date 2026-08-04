namespace Identity.Api.Endpoints;

/// <summary>
/// Minimal OIDC discovery document — just enough for ASP.NET Core's automatic Authority-based JWT
/// validation to find the issuer and JWKS. Every property has an explicit <see cref="JsonPropertyNameAttribute"/>
/// rather than relying on the service-wide camelCase JSON policy — OIDC field names are spec-fixed
/// snake_case (e.g. <c>response_types_supported</c>), and a coincidental camelCase match would
/// silently break every consuming service's discovery fetch if that policy ever changed.
/// </summary>
/// <param name="Issuer">The issuer identifier — must exactly match the <c>iss</c> claim on every minted token.</param>
/// <param name="JwksUri">Where the JWKS document is published.</param>
/// <param name="TokenEndpoint">Informational only — this service has no separate authorization-code flow; verification IS token issuance.</param>
/// <param name="ResponseTypesSupported">OIDC-required field — the supported <c>response_type</c> values.</param>
/// <param name="SubjectTypesSupported">OIDC-required field — the supported subject identifier types.</param>
/// <param name="IdTokenSigningAlgValuesSupported">OIDC-required field — always <c>["RS256"]</c> here.</param>
public sealed record OidcDiscoveryDocument(
    [property: JsonPropertyName("issuer")] string Issuer,
    [property: JsonPropertyName("jwks_uri")] string JwksUri,
    [property: JsonPropertyName("token_endpoint")] string TokenEndpoint,
    [property: JsonPropertyName("response_types_supported")] IReadOnlyList<string> ResponseTypesSupported,
    [property: JsonPropertyName("subject_types_supported")] IReadOnlyList<string> SubjectTypesSupported,
    [property: JsonPropertyName("id_token_signing_alg_values_supported")] IReadOnlyList<string> IdTokenSigningAlgValuesSupported);
