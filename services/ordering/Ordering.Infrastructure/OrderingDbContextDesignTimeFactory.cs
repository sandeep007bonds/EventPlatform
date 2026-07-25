namespace Ordering.Infrastructure;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build an <see cref="OrderingDbContext"/> without
/// starting the API host. Used only by the EF tooling — never at runtime. Reads the connection
/// string from <c>ORDERING_DB</c>, falling back to the local dev database.
/// </summary>
internal sealed class OrderingDbContextDesignTimeFactory : IDesignTimeDbContextFactory<OrderingDbContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=eventplatform;Username=eventplatform;Password=localdev";

    /// <inheritdoc />
    public OrderingDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ORDERING_DB") ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new OrderingDbContext(options);
    }
}
