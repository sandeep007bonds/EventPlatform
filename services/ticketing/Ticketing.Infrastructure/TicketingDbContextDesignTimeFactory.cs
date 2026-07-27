namespace Ticketing.Infrastructure;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build a <see cref="TicketingDbContext"/> without
/// starting the API host. Used only by the EF tooling — never at runtime. Reads the connection
/// string from <c>TICKETING_DB</c>, falling back to the local dev database.
/// </summary>
internal sealed class TicketingDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TicketingDbContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=ticketing;Username=eventplatform;Password=localdev";

    /// <inheritdoc />
    public TicketingDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("TICKETING_DB") ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new TicketingDbContext(options);
    }
}
