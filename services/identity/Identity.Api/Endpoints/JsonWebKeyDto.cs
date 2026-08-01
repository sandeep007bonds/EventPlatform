namespace Identity.Api.Endpoints;

/// <summary>One RSA public key entry, RFC 7517 shape.</summary>
/// <param name="KeyType">Always <c>"RSA"</c>.</param>
/// <param name="Use">Always <c>"sig"</c> (signature verification).</param>
/// <param name="KeyId">Matches the <c>kid</c> header on tokens signed with this key.</param>
/// <param name="Algorithm">Always <c>"RS256"</c>.</param>
/// <param name="Modulus">The RSA modulus, base64url-encoded.</param>
/// <param name="Exponent">The RSA public exponent, base64url-encoded.</param>
public sealed record JsonWebKeyDto(
    [property: JsonPropertyName("kty")] string KeyType,
    [property: JsonPropertyName("use")] string Use,
    [property: JsonPropertyName("kid")] string KeyId,
    [property: JsonPropertyName("alg")] string Algorithm,
    [property: JsonPropertyName("n")] string Modulus,
    [property: JsonPropertyName("e")] string Exponent);
