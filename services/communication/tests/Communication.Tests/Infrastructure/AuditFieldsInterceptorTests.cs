namespace Communication.Tests.Infrastructure;

/// <summary>
/// Integration test for the audit-field interceptor, against a real Postgres container.
/// </summary>
/// <remarks>
/// It has to be an integration test. The four audit fields are EF shadow properties (ADR-0036), so
/// they exist only in the model — no entity class has them, and no domain unit test can observe
/// one. The only way to assert "the row was stamped" is to save a row and read it back.
/// </remarks>
public sealed class AuditFieldsInterceptorTests : IAsyncLifetime
{
    private const string UserActor = "6f9619ff-8b86-d011-b42d-00c04fc964ff";
    private const string ServiceActor = "service:communication";

    // Pinned to the image docker-compose runs, not the Testcontainers module default. Two reasons,
    // and the second is why this file changed: the default is an older Postgres than production
    // uses, so the tests were proving the wrong version; and the default is an image nothing else
    // pulls, so it is never in the local cache and every run depends on a registry fetch that can
    // rate-limit or fail. Keep this in step with docker-compose.yml.
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        await using var dbContext = NewDbContext(UserActor);
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await container.DisposeAsync();

    [Fact]
    public async Task InsertedRow_IsStampedWithTheActorAndTheTime()
    {
        var before = DateTimeOffset.UtcNow;
        var entryId = await InsertDeliveryLogEntryAsync(UserActor);

        await using var dbContext = NewDbContext(UserActor);
        var reloaded = await dbContext.DeliveryLog.SingleAsync(e => e.Id == entryId);
        var audit = dbContext.Entry(reloaded);

        audit.Property<string?>(AuditFieldNames.CreatedBy).CurrentValue.ShouldBe(UserActor);
        audit.Property<string?>(AuditFieldNames.UpdatedBy).CurrentValue.ShouldBe(UserActor);

        var createdAt = audit.Property<DateTimeOffset?>(AuditFieldNames.CreatedAt).CurrentValue;
        createdAt.ShouldNotBeNull();
        createdAt.Value.ShouldBeInRange(before, DateTimeOffset.UtcNow);

        // Equal on insert, per AuditFieldNames.UpdatedAt's contract — "equal to CreatedAt until
        // first update" — so a query for "recently touched" needs only one column.
        audit.Property<DateTimeOffset?>(AuditFieldNames.UpdatedAt).CurrentValue.ShouldBe(createdAt);
    }

    [Fact]
    public async Task UpdatedRow_MovesTheUpdatedFieldsAndLeavesTheCreatedFieldsAlone()
    {
        var entryId = await InsertDeliveryLogEntryAsync(UserActor);

        DateTimeOffset? createdAt;
        await using (var reader = NewDbContext(UserActor))
        {
            var original = await reader.DeliveryLog.SingleAsync(e => e.Id == entryId);
            createdAt = reader.Entry(original).Property<DateTimeOffset?>(AuditFieldNames.CreatedAt).CurrentValue;
        }

        // A second actor, so "CreatedBy survived" is a real assertion rather than both fields
        // happening to hold the same string.
        await using (var writer = NewDbContext(ServiceActor))
        {
            var entry = await writer.DeliveryLog.SingleAsync(e => e.Id == entryId);

            // DeliveryLogEntry is deliberately immutable — there is no setter to touch — so the
            // Modified state is set directly. That is precisely the interceptor branch under test.
            writer.Entry(entry).State = EntityState.Modified;
            await writer.SaveChangesAsync(CancellationToken.None);
        }

        await using var dbContext = NewDbContext(UserActor);
        var reloaded = await dbContext.DeliveryLog.SingleAsync(e => e.Id == entryId);
        var audit = dbContext.Entry(reloaded);

        audit.Property<string?>(AuditFieldNames.CreatedBy).CurrentValue.ShouldBe(UserActor);
        audit.Property<DateTimeOffset?>(AuditFieldNames.CreatedAt).CurrentValue.ShouldBe(createdAt);

        audit.Property<string?>(AuditFieldNames.UpdatedBy).CurrentValue.ShouldBe(ServiceActor);
        audit.Property<DateTimeOffset?>(AuditFieldNames.UpdatedAt).CurrentValue.ShouldNotBeNull();
        audit.Property<DateTimeOffset?>(AuditFieldNames.UpdatedAt).CurrentValue!.Value
            .ShouldBeGreaterThanOrEqualTo(createdAt!.Value);
    }

    [Fact]
    public async Task WriteWithNoUser_IsAttributedToTheService_NotLeftNull()
    {
        // The case ADR-0036 cares most about: the checkout saga, the expired-hold reaper and every
        // Dapr subscriber write with no ClaimsPrincipal at all. Recording those as null would make
        // the trail lie by omission, so HttpAuditContext falls back to the service identity.
        var entryId = await InsertDeliveryLogEntryAsync(ServiceActor);

        await using var dbContext = NewDbContext(ServiceActor);
        var reloaded = await dbContext.DeliveryLog.SingleAsync(e => e.Id == entryId);

        dbContext.Entry(reloaded).Property<string?>(AuditFieldNames.CreatedBy).CurrentValue
            .ShouldBe(ServiceActor);
    }

    private async Task<Guid> InsertDeliveryLogEntryAsync(string actor)
    {
        await using var dbContext = NewDbContext(actor);

        var entry = DeliveryLogEntry.Sent(
            Guid.NewGuid(),
            NotificationChannel.Email,
            "buyer@example.com",
            "order-confirmed",
            "dev-log",
            providerReference: null,
            correlationId: null);

        dbContext.DeliveryLog.Add(entry);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        return entry.Id;
    }

    // The interceptor is attached here rather than coming from DI, because this test constructs its
    // DbContext directly — the same way ProcessedNotificationEventTests does. UseAuditFields is the
    // production path and takes an IServiceProvider, so the interceptor is built directly instead.
    private CommunicationDbContext NewDbContext(string actor)
    {
        var auditContext = Substitute.For<IAuditContext>();
        auditContext.Actor.Returns(actor);
        auditContext.ActorType.Returns(
            actor.StartsWith("service:", StringComparison.Ordinal) ? ActorType.Service : ActorType.User);

        var options = new DbContextOptionsBuilder<CommunicationDbContext>()
            .UseNpgsql(container.GetConnectionString())
            .AddInterceptors(new AuditFieldsInterceptor(auditContext))
            .Options;

        return new CommunicationDbContext(options);
    }
}
