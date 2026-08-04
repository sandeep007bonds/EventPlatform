namespace Identity.Infrastructure.Signing;

/// <summary>
/// Vends the RSA key used to sign new tokens and the public keys used to build the JWKS document.
/// Config-gated selection (<c>Identity:Jwt:SigningKeySource</c>) mirrors Communication's
/// vendor-adapter gate in shape (raw <see cref="IConfiguration"/> indexer, never
/// <c>IsDevelopment()</c>) but NOT in fail-open behavior: requesting <c>KeyVault</c> without an
/// implementation throws at startup rather than silently falling back — a wrong key-custody model
/// for something security-sensitive should never be a silent degrade (unlike Communication's
/// vendor gate, where "unset" safely means "log instead of send," with no security implication).
/// Referenced directly by <c>Identity.Api</c>'s discovery/JWKS endpoints — not routed through
/// <c>Identity.Application</c>, since nothing there ever needs "the signing key" itself.
/// </summary>
public interface ISigningKeyProvider
{
    /// <summary>Returns the key currently used to sign new tokens.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<ActiveSigningKey> GetActiveKeyAsync(CancellationToken cancellationToken);

    /// <summary>Returns every key whose public half should currently be published in the JWKS document.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<IReadOnlyList<PublicSigningKey>> GetPublicKeysAsync(CancellationToken cancellationToken);
}
