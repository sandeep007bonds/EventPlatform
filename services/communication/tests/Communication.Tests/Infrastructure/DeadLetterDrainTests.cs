namespace Communication.Tests.Infrastructure;

/// <summary>
/// The dead-letter drain, against a real Postgres container.
/// </summary>
/// <remarks>
/// Tested here rather than in the building blocks' own project because it needs a real database and
/// this is where the Testcontainers fixture already lives. Communication is a fair stand-in: it
/// subscribes to three topics and publishes nothing, which is exactly the case that made the
/// dead-letter store separate from the outbox in the first place (ADR-0040).
/// </remarks>
public sealed class DeadLetterDrainTests : IAsyncLifetime
{
    // Pinned to the image docker-compose runs, for the reasons ProcessedNotificationEventTests
    // spells out. Keep this in step with docker-compose.yml.
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

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
    public async Task ADeadLetter_IsRecordedWithItsWholeEnvelope()
    {
        var envelope = NewEnvelope();
        var body = envelope.AttachTo(JsonNode.Parse("""{"eventId":"11111111-1111-1111-1111-111111111111"}"""));

        await Drain().RecordAsync("OrderConfirmed", body, CancellationToken.None);

        var stored = await dbContext.DeadLetterMessages.SingleAsync();
        stored.MessageId.ShouldBe(envelope.MessageId);
        stored.CorrelationId.ShouldBe(envelope.CorrelationId);
        stored.CausationId.ShouldBe(envelope.CausationId);
        stored.Topic.ShouldBe("OrderConfirmed");
        stored.ResolvedAt.ShouldBeNull();

        // Verbatim, not reshaped: whatever made the message unhandleable may well be in the part a
        // parser would drop, and the point of the record is to show what actually arrived.
        stored.Payload.ShouldContain("11111111-1111-1111-1111-111111111111");
    }

    // The drain is a subscriber like any other, so Dapr can deliver the same dead letter twice.
    [Fact]
    public async Task TheSameDeadLetterTwice_IsRecordedOnce()
    {
        var body = NewEnvelope().AttachTo(JsonNode.Parse("""{"eventId":"11111111-1111-1111-1111-111111111111"}"""));

        await Drain().RecordAsync("OrderConfirmed", body, CancellationToken.None);
        await Drain().RecordAsync("OrderConfirmed", body, CancellationToken.None);

        (await dbContext.DeadLetterMessages.CountAsync()).ShouldBe(1);
    }

    // A message malformed enough to lose its envelope is exactly the kind that ends up here, so it
    // is recorded rather than rejected — noisily, since there is no id to dedupe on.
    [Fact]
    public async Task AMessageWithNoEnvelope_IsStillRecorded()
    {
        var body = JsonNode.Parse("""{"nonsense":true}""");

        await Drain().RecordAsync("Unknown", body, CancellationToken.None);

        var stored = await dbContext.DeadLetterMessages.SingleAsync();
        stored.MessageId.ShouldBe(Guid.Empty);
        stored.CorrelationId.ShouldBe(Guid.Empty);
        stored.CausationId.ShouldBeNull();
    }

    [Fact]
    public async Task TwoUnenvelopedMessages_AreBothRecordedRatherThanCollapsed()
    {
        await Drain().RecordAsync("Unknown", JsonNode.Parse("""{"a":1}"""), CancellationToken.None);
        await Drain().RecordAsync("Unknown", JsonNode.Parse("""{"b":2}"""), CancellationToken.None);

        (await dbContext.DeadLetterMessages.CountAsync()).ShouldBe(2);
    }

    private static EventEnvelope NewEnvelope() => new(
        MessageId: Guid.CreateVersion7(),
        CorrelationId: Guid.CreateVersion7(),
        CausationId: Guid.CreateVersion7(),
        EventType: "OrderConfirmed",
        EventVersion: 1,
        OccurredAt: DateTimeOffset.UtcNow,
        TenantId: Guid.CreateVersion7());

    private DeadLetterDrain Drain() => new(dbContext, NullLogger<DeadLetterDrain>.Instance);
}
