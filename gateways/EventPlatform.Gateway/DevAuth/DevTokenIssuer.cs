namespace EventPlatform.Gateway.DevAuth;

/// <summary>
/// Issues Development-only JWTs matching the claim shape and signing scheme every backend service
/// already validates (the <c>DevSigningKey</c> path in <c>AuthenticationExtensions.cs</c>) — a
/// safe, server-side stand-in for <c>scripts/dev-token.sh</c>'s client-side minting, which a
/// browser cannot replicate without shipping the shared signing secret to the client.
/// </summary>
public static class DevTokenIssuer
{
    private const string DefaultIssuer = "eventplatform-dev";
    private const string DefaultAudience = "eventplatform";
    private const string DefaultTenantId = "11111111-1111-1111-1111-111111111111";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    /// <summary>Issues a token for the given dev-login request.</summary>
    /// <param name="request">The dev-login request.</param>
    /// <param name="signingKey">The shared HS256 signing key (<c>Jwt:DevSigningKey</c>).</param>
    /// <param name="issuer">The token issuer; defaults to <c>eventplatform-dev</c>.</param>
    /// <param name="audience">The token audience; defaults to <c>eventplatform</c>.</param>
    /// <returns>The issued token, its expiry, and the resolved user.</returns>
    public static DevTokenResult Issue(DevLoginRequest request, string signingKey, string? issuer, string? audience)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Email);
        ArgumentException.ThrowIfNullOrWhiteSpace(signingKey);

        var sub = DeriveSubFromEmail(request.Email);
        var tenantId = request.TenantId ?? Guid.Parse(DefaultTenantId);
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(TokenLifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, sub.ToString()),
            new("tenant_id", tenantId.ToString()),
            new(JwtRegisteredClaimNames.Email, request.Email),
        };

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            claims.Add(new Claim("role", request.Role));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer ?? DefaultIssuer,
            audience ?? DefaultAudience,
            claims,
            now.UtcDateTime,
            expiresAt.UtcDateTime,
            credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var user = new DevLoginUser(sub, request.Email, tenantId, request.Role);

        return new DevTokenResult(accessToken, expiresAt, user);
    }

    // Deterministic so the same email always maps to the same id across dev sessions — Inventory
    // holds and Ordering checkout key off `sub`, so a stable id matters even in dev. Not a security
    // mechanism; this is a Development-only convenience, never used in Production.
    private static Guid DeriveSubFromEmail(string email)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant()));
        return new Guid(hash[..16]);
    }
}
