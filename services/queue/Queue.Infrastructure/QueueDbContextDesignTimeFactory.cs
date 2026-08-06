namespace Queue.Infrastructure;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build a <see cref="QueueDbContext"/> without
/// starting the API host. Used only by the EF tooling — never at runtime. Reads the connection
/// string from <c>QUEUE_DB</c>, falling back to the local dev database.
/// </summary>
internal sealed class QueueDbContextDesignTimeFactory : IDesignTimeDbContextFactory<QueueDbContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=queue;Username=eventplatform;Password=localdev";

    /// <inheritdoc />
    public QueueDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("QUEUE_DB") ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<QueueDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new QueueDbContext(options);
    }
}
