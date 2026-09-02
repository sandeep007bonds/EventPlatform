namespace Venues.Infrastructure;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build a <see cref="VenuesDbContext"/> without
/// starting the API host (which would spin up Dapr, the outbox relay, etc.). Used only by the EF
/// tooling — never at runtime. Reads the connection string from <c>VENUE_DB</c>, falling back to
/// the local dev database.
/// </summary>
internal sealed class VenuesDbContextDesignTimeFactory : IDesignTimeDbContextFactory<VenuesDbContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=venue;Username=eventplatform;Password=localdev";

    /// <inheritdoc />
    public VenuesDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("VENUE_DB") ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<VenuesDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new VenuesDbContext(options);
    }
}
