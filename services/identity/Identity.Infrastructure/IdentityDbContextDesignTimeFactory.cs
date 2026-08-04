namespace Identity.Infrastructure;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build an <see cref="IdentityDbContext"/> without
/// starting the API host. Used only by the EF tooling — never at runtime. Reads the connection
/// string from <c>IDENTITY_DB</c>, falling back to the local dev database.
/// </summary>
internal sealed class IdentityDbContextDesignTimeFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=identity;Username=eventplatform;Password=localdev";

    /// <inheritdoc />
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("IDENTITY_DB") ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new IdentityDbContext(options);
    }
}
