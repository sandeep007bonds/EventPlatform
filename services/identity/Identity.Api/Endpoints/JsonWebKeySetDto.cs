namespace Identity.Api.Endpoints;

/// <summary>The JWKS document — RFC 7517 shape.</summary>
/// <param name="Keys">The published public keys.</param>
public sealed record JsonWebKeySetDto([property: JsonPropertyName("keys")] IReadOnlyList<JsonWebKeyDto> Keys);
