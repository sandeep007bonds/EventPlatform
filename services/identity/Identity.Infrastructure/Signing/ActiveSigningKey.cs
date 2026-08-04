namespace Identity.Infrastructure.Signing;

/// <summary>The active RSA signing key, ready to build <see cref="SigningCredentials"/> from.</summary>
/// <param name="Kid">The key id, used as the JWT/JWKS <c>kid</c>.</param>
/// <param name="Rsa">The RSA key pair.</param>
public sealed record ActiveSigningKey(string Kid, RSA Rsa);
