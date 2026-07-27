namespace Payments.Infrastructure;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build a <see cref="PaymentDbContext"/> without
/// starting the API host. Used only by the EF tooling — never at runtime. Reads the connection
/// string from <c>PAYMENTS_DB</c>, falling back to the local dev database.
/// </summary>
internal sealed class PaymentDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PaymentDbContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=payments;Username=eventplatform;Password=localdev";

    /// <inheritdoc />
    public PaymentDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("PAYMENTS_DB") ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new PaymentDbContext(options);
    }
}
