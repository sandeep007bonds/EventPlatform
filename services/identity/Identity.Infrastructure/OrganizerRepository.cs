namespace Identity.Infrastructure;

/// <summary>EF Core implementation of <see cref="IOrganizerRepository"/>.</summary>
/// <param name="dbContext">The Identity database context.</param>
internal sealed class OrganizerRepository(IdentityDbContext dbContext) : IOrganizerRepository
{
    /// <inheritdoc />
    public void AddTenant(Tenant tenant) => dbContext.Tenants.Add(tenant);

    /// <inheritdoc />
    public void AddOrganizerAccount(OrganizerAccount account) => dbContext.OrganizerAccounts.Add(account);

    /// <inheritdoc />
    public Task<OrganizerAccount?> GetOrganizerByEmailAsync(string email, CancellationToken cancellationToken) =>
        dbContext.OrganizerAccounts.FirstOrDefaultAsync(a => a.Email == email, cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
