namespace Identity.Infrastructure;

/// <summary>EF Core implementation of <see cref="IIdentityRepository"/>.</summary>
/// <param name="dbContext">The Identity database context.</param>
internal sealed class IdentityRepository(IdentityDbContext dbContext) : IIdentityRepository
{
    /// <inheritdoc />
    public void AddPhoneVerification(PhoneVerification verification) =>
        dbContext.PhoneVerifications.Add(verification);

    /// <inheritdoc />
    public Task<PhoneVerification?> GetLatestPhoneVerificationAsync(string phoneNumber, CancellationToken cancellationToken) =>
        dbContext.PhoneVerifications
            .Where(v => v.PhoneNumber == phoneNumber)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public Task<BuyerAccount?> GetBuyerAccountByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken) =>
        dbContext.BuyerAccounts.FirstOrDefaultAsync(a => a.PhoneNumber == phoneNumber, cancellationToken);

    /// <inheritdoc />
    public void AddBuyerAccount(BuyerAccount account) => dbContext.BuyerAccounts.Add(account);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
