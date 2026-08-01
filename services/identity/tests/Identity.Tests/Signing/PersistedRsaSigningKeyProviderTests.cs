namespace Identity.Tests.Signing;

/// <summary>Integration test against a real Postgres container proving the "restart survives" guarantee.</summary>
public sealed class PersistedRsaSigningKeyProviderTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync() => await container.StartAsync();

    public async Task DisposeAsync() => await container.DisposeAsync();

    [Fact]
    public async Task GetActiveKeyAsync_FirstCall_GeneratesAndPersistsAKey()
    {
        await using var dbContext = await CreateDbContextAsync();
        var provider = CreateProvider(dbContext);

        var key = await provider.GetActiveKeyAsync(CancellationToken.None);

        key.Kid.ShouldNotBeNullOrWhiteSpace();
        (await dbContext.SigningKeys.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task GetActiveKeyAsync_SecondProviderInstance_LoadsTheSamePersistedKey_RatherThanRegenerating()
    {
        await using var firstDbContext = await CreateDbContextAsync();
        var firstProvider = CreateProvider(firstDbContext);
        var firstKey = await firstProvider.GetActiveKeyAsync(CancellationToken.None);

        // A brand-new provider instance (simulating a fresh process) against the same database.
        await using var secondDbContext = await CreateDbContextAsync(reuseSchema: true);
        var secondProvider = CreateProvider(secondDbContext);
        var secondKey = await secondProvider.GetActiveKeyAsync(CancellationToken.None);

        secondKey.Kid.ShouldBe(firstKey.Kid);
        (await secondDbContext.SigningKeys.CountAsync()).ShouldBe(1);
    }

    private async Task<IdentityDbContext> CreateDbContextAsync(bool reuseSchema = false)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(container.GetConnectionString())
            .Options;

        var dbContext = new IdentityDbContext(options);
        if (!reuseSchema)
        {
            await dbContext.Database.EnsureCreatedAsync();
        }

        return dbContext;
    }

    private static PersistedRsaSigningKeyProvider CreateProvider(IdentityDbContext dbContext)
    {
        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        services.AddScoped<ISigningKeyRepository, SigningKeyRepository>();
        var provider = services.BuildServiceProvider();

        return new PersistedRsaSigningKeyProvider(provider.GetRequiredService<IServiceScopeFactory>());
    }
}
