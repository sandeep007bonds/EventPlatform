namespace Identity.Domain;

/// <summary>
/// A persisted RSA signing key. <see cref="Id"/> doubles as the JWT/JWKS <c>kid</c> so a future
/// rotation (a second row, old one kept non-active until its outstanding tokens expire) needs no
/// schema change. Exactly one row should have <see cref="IsActive"/> true at a time this pass — no
/// rotation is implemented yet (see the ADR-0016 extension).
/// </summary>
public sealed class SigningKey
{
    private SigningKey()
    {
    }

    private SigningKey(string privateKeyPkcs8Base64)
    {
        Id = Guid.CreateVersion7();
        PrivateKeyPkcs8Base64 = privateKeyPkcs8Base64;
        CreatedAt = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    /// <summary>The key's id — also used as the JWT/JWKS <c>kid</c>.</summary>
    public Guid Id { get; private set; }

    /// <summary>The RSA private key, PKCS8 DER-encoded then base64'd, for storage as text.</summary>
    public string PrivateKeyPkcs8Base64 { get; private set; } = default!;

    /// <summary>When this key was generated.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Whether this key is the one currently used to sign new tokens.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Generates a new 2048-bit RSA key pair and wraps it as a new active <see cref="SigningKey"/>.
    /// The private key material never leaves this process except as opaque PKCS8 bytes destined
    /// for the <c>identity</c> Postgres database — see the ADR-0016 extension for why the DB (not a
    /// file, not an env var) is this pass's chosen store.
    /// </summary>
    /// <returns>A new, active <see cref="SigningKey"/>.</returns>
    public static SigningKey Generate()
    {
        using var rsa = RSA.Create(2048);
        var pkcs8 = rsa.ExportPkcs8PrivateKey();
        return new SigningKey(Convert.ToBase64String(pkcs8));
    }
}
