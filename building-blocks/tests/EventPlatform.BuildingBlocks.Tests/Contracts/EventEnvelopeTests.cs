namespace EventPlatform.BuildingBlocks.Tests.Contracts;

/// <summary>
/// The envelope's write and read sides, tested as the round trip they are.
/// </summary>
/// <remarks>
/// The property name is written by the outbox relay and read by every subscriber. Nothing fails
/// loudly if those two disagree — the envelope is simply never found, every message looks like the
/// start of its own chain, and the whole point of ADR-0040 quietly evaporates. Testing the pair
/// together is what makes that impossible.
/// </remarks>
public sealed class EventEnvelopeTests
{
    private static readonly EventEnvelope Sample = new(
        MessageId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
        CorrelationId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
        CausationId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
        EventType: "SeatSold",
        EventVersion: 1,
        OccurredAt: new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.Zero),
        TenantId: Guid.Parse("44444444-4444-4444-4444-444444444444"));

    [Fact]
    public void AnAttachedEnvelope_ReadsBackIdentically()
    {
        var published = Sample.AttachTo(JsonNode.Parse("""{"eventId":"55555555-5555-5555-5555-555555555555"}"""));

        EventEnvelope.TryRead(published, out var read).ShouldBeTrue();
        read.ShouldBe(Sample);
    }

    // The reason the envelope can sit beside the event rather than inside it: a consumer binding
    // its own typed record must not notice the extra property. If this ever fails, all eleven
    // subscribers break at once.
    [Fact]
    public void AttachingAnEnvelope_LeavesTheEventItselfUntouched()
    {
        var published = Sample.AttachTo(JsonNode.Parse("""{"eventId":"55555555-5555-5555-5555-555555555555","seatId":"a"}"""));

        published["eventId"]!.GetValue<string>().ShouldBe("55555555-5555-5555-5555-555555555555");
        published["seatId"]!.GetValue<string>().ShouldBe("a");
    }

    [Fact]
    public void AnEnvelopeWithNoCausation_SurvivesTheRoundTrip()
    {
        var startOfChain = Sample with { CausationId = null };

        var published = startOfChain.AttachTo(JsonNode.Parse("""{"eventId":"55555555-5555-5555-5555-555555555555"}"""));

        EventEnvelope.TryRead(published, out var read).ShouldBeTrue();
        read!.CausationId.ShouldBeNull();
    }

    [Fact]
    public void AMessageWithNoEnvelope_IsReportedRatherThanThrowing()
    {
        var body = JsonNode.Parse("""{"eventId":"55555555-5555-5555-5555-555555555555"}""");

        EventEnvelope.TryRead(body, out var read).ShouldBeFalse();
        read.ShouldBeNull();
    }

    [Fact]
    public void AMalformedEnvelope_IsReportedRatherThanThrowing()
    {
        var body = JsonNode.Parse("""{"envelope":{"messageId":"not-a-guid"}}""");

        EventEnvelope.TryRead(body, out var read).ShouldBeFalse();
        read.ShouldBeNull();
    }

    [Fact]
    public void ANullBody_IsReportedRatherThanThrowing()
    {
        EventEnvelope.TryRead(null, out var read).ShouldBeFalse();
        read.ShouldBeNull();
    }

    // Deliberate: losing an envelope on one message is a gap in a trail, while dropping the message
    // is a lost sale. No contract serializes to a non-object today, but the relay cannot promise it.
    [Fact]
    public void APayloadThatIsNotAnObject_IsPublishedUnchangedRatherThanDropped()
    {
        var published = Sample.AttachTo(JsonNode.Parse("[1,2,3]"));

        published.ShouldBeOfType<JsonArray>();
        ((JsonArray)published).Count.ShouldBe(3);
    }
}
