namespace Catalog.Infrastructure;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build a <see cref="CatalogDbContext"/> without
/// starting the API host (which would spin up Dapr, the outbox relay, etc.). Used only by the EF
/// tooling — never at runtime. Reads the connection string from <c>CATALOG_DB</c>, falling back to
/// the local dev database.
/// </summary>
internal sealed class CatalogDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=catalog;Username=eventplatform;Password=localdev";

    /// <inheritdoc />
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("CATALOG_DB") ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new CatalogDbContext(options);
    }
}
