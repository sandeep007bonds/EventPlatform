namespace Inventory.Infrastructure;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build an <see cref="InventoryDbContext"/> without
/// starting the API host. Used only by the EF tooling — never at runtime. Reads the connection
/// string from <c>INVENTORY_DB</c>, falling back to the local dev database.
/// </summary>
internal sealed class InventoryDbContextDesignTimeFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=inventory;Username=eventplatform;Password=localdev";

    /// <inheritdoc />
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("INVENTORY_DB") ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new InventoryDbContext(options);
    }
}
