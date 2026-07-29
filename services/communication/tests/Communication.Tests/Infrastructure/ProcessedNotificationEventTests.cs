namespace Communication.Tests.Infrastructure;

/// <summary>Integration test against a real Postgres container proving the dedup ledger's DB-level guarantee.</summary>
public sealed class ProcessedNotificationEventTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder().Build();
    private CommunicationDbContext dbContext = default!;

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        var options = new DbContextOptionsBuilder<CommunicationDbContext>()
            .UseNpgsql(container.GetConnectionString())
            .Options;

        dbContext = new CommunicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await container.DisposeAsync();
    }

    [Fact]
    public async Task RecordedEvent_IsReportedAsProcessed()
    {
        var repository = new NotificationRepository(dbContext);
        var eventId = Guid.NewGuid();

        (await repository.HasProcessedEventAsync(eventId, CancellationToken.None)).ShouldBeFalse();

        repository.RecordProcessedEvent(eventId, nameof(OrderConfirmed));
        await repository.SaveChangesAsync(CancellationToken.None);

        (await repository.HasProcessedEventAsync(eventId, CancellationToken.None)).ShouldBeTrue();
    }

    [Fact]
    public async Task RedeliveredEventId_ViolatesUniqueConstraint()
    {
        var eventId = Guid.NewGuid();

        var firstRepository = new NotificationRepository(dbContext);
        firstRepository.RecordProcessedEvent(eventId, nameof(OrderConfirmed));
        await firstRepository.SaveChangesAsync(CancellationToken.None);

        // A second insert of the same EventId (simulating a subscriber that skipped the
        // HasProcessedEventAsync check) must fail at the database level — this is the guarantee
        // IntegrationEventNotificationHandler's dedup check exists to avoid ever hitting in practice.
        var options = new DbContextOptionsBuilder<CommunicationDbContext>().UseNpgsql(container.GetConnectionString()).Options;
        await using var secondDbContext = new CommunicationDbContext(options);
        var secondRepository = new NotificationRepository(secondDbContext);
        secondRepository.RecordProcessedEvent(eventId, nameof(OrderConfirmed));

        await Should.ThrowAsync<DbUpdateException>(() => secondRepository.SaveChangesAsync(CancellationToken.None));
    }
}
