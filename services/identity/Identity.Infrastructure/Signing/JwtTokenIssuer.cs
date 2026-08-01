namespace Identity.Infrastructure.Signing;

/// <summary>
/// Builds and signs the buyer access token. Claims: <c>sub</c> (the buyer id), <c>iss</c>/<c>aud</c>
/// (from config — <c>aud</c> must equal <c>"eventplatform"</c>, the value every one of the 5
/// existing services already expects, so no change is needed on their side), <c>iat</c>/<c>nbf</c>/
/// <c>exp</c>, and <c>role: "buyer"</c> (parity with dev-login's role claim — not enforced by any
/// authorization policy today). Deliberately OMITS <c>tenant_id</c> (ADR-0022): buyers aren't
/// tenant-scoped, and the two endpoints that used to require that claim were already reworked to
/// derive tenant from the resource instead, specifically so a token like this one can work.
/// </summary>
/// <param name="issuer">The <c>iss</c> claim value — must exactly match the discovery document's <c>issuer</c> field.</param>
/// <param name="audience">The <c>aud</c> claim value.</param>
/// <param name="lifetime">The access token's lifetime (recommend ~7 days — see the ADR-0016 extension).</param>
/// <param name="signingKeyProvider">Vends the RSA key to sign with.</param>
internal sealed class JwtTokenIssuer(string issuer, string audience, TimeSpan lifetime, ISigningKeyProvider signingKeyProvider) : ITokenIssuer
{
    /// <inheritdoc />
    public async Task<IssuedAccessToken> IssueAsync(Guid buyerId, CancellationToken cancellationToken)
    {
        var activeKey = await signingKeyProvider.GetActiveKeyAsync(cancellationToken);
        var credentials = new SigningCredentials(new RsaSecurityKey(activeKey.Rsa) { KeyId = activeKey.Kid }, SecurityAlgorithms.RsaSha256);

        var now = DateTime.UtcNow;
        var expires = now.Add(lifetime);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = credentials,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, buyerId.ToString()),
                new Claim("role", "buyer"),
            ]),
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateEncodedJwt(descriptor);
        return new IssuedAccessToken(token, expires);
    }
}
