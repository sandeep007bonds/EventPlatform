namespace Communication.Infrastructure;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build a <see cref="CommunicationDbContext"/> without
/// starting the API host. Used only by the EF tooling — never at runtime. Reads the connection
/// string from <c>COMMUNICATION_DB</c>, falling back to the local dev database.
/// </summary>
internal sealed class CommunicationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CommunicationDbContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=communication;Username=eventplatform;Password=localdev";

    /// <inheritdoc />
    public CommunicationDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("COMMUNICATION_DB") ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<CommunicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new CommunicationDbContext(options);
    }
}
