namespace Identity.Infrastructure.Signing;

/// <summary>A public key entry for the JWKS document.</summary>
/// <param name="Kid">The key id.</param>
/// <param name="PublicParameters">The RSA public key parameters (modulus/exponent).</param>
public sealed record PublicSigningKey(string Kid, RSAParameters PublicParameters);
