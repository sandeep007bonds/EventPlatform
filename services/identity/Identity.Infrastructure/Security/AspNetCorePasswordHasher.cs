namespace Identity.Infrastructure.Security;

/// <summary>
/// Adapts <c>Microsoft.Extensions.Identity.Core</c>'s <see cref="PasswordHasher{TUser}"/> — a
/// vetted, battle-tested primitive — behind Identity's own <see cref="IPasswordHasher"/> port,
/// without pulling in ASP.NET Core Identity's full cookie/UI/EF-store stack.
/// </summary>
/// <param name="inner">The underlying ASP.NET Core Identity password hasher.</param>
internal sealed class AspNetCorePasswordHasher(PasswordHasher<OrganizerAccount> inner) : IPasswordHasher
{
    // The default PasswordHasher<TUser> implementation never reads its `user` argument — hashing
    // happens before an OrganizerAccount exists (chicken-and-egg at registration), so null is safe here.
    /// <inheritdoc />
    public string Hash(string password) => inner.HashPassword(user: null!, password);

    /// <inheritdoc />
    public bool Verify(OrganizerAccount account, string password)
    {
        var result = inner.VerifyHashedPassword(account, account.PasswordHash, password);

        // SuccessRehashNeeded (e.g. after an iteration-count upgrade) still means the password
        // matched — this pass doesn't act on the rehash signal, a documented, deliberate no-op
        // (see ADR-0023), not a correctness gap.
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
