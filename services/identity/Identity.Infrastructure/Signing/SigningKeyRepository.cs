namespace Identity.Infrastructure.Signing;

/// <summary>EF Core implementation of <see cref="ISigningKeyRepository"/>.</summary>
/// <param name="dbContext">The Identity database context.</param>
internal sealed class SigningKeyRepository(IdentityDbContext dbContext) : ISigningKeyRepository
{
    /// <inheritdoc />
    public Task<SigningKey?> GetActiveAsync(CancellationToken cancellationToken) =>
        dbContext.SigningKeys.FirstOrDefaultAsync(k => k.IsActive, cancellationToken);

    /// <inheritdoc />
    public void Add(SigningKey key) => dbContext.SigningKeys.Add(key);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
