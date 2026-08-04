namespace Identity.Infrastructure;

/// <summary>
/// EF Core database context for the Identity service (schema <c>identity</c>). Like Communication,
/// this does not implement an outbox contract — Identity never publishes an integration event.
/// </summary>
/// <param name="options">The context options.</param>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    /// <summary>OTP challenges issued to phone numbers.</summary>
    public DbSet<PhoneVerification> PhoneVerifications => Set<PhoneVerification>();

    /// <summary>Durable buyer identities.</summary>
    public DbSet<BuyerAccount> BuyerAccounts => Set<BuyerAccount>();

    /// <summary>Organizations, created via self-service organizer registration.</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>Durable organizer identities (email+password, tenant-scoped).</summary>
    public DbSet<OrganizerAccount> OrganizerAccounts => Set<OrganizerAccount>();

    /// <summary>Persisted RSA signing keys.</summary>
    internal DbSet<SigningKey> SigningKeys => Set<SigningKey>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("identity");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
