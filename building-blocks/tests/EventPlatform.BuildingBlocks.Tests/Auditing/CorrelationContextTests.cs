namespace EventPlatform.BuildingBlocks.Tests.Auditing;

/// <summary>
/// The correlation holder's one hard guarantee: it never hands back an empty id.
/// </summary>
/// <remarks>
/// An empty GUID would be worse than no column at all — it looks like data, sorts and groups like
/// data, and would silently collapse every unattributed piece of work in the platform into one
/// enormous fake "chain". The self-seeding here is what prevents that, and it matters most on the
/// paths nobody thinks about: the expired-hold reaper, the outbox relay, a workflow activity.
/// </remarks>
public sealed class CorrelationContextTests
{
    [Fact]
    public void AContextNobodyPopulated_StillHasAnId()
    {
        var context = new CorrelationContext();

        context.CorrelationId.ShouldNotBe(Guid.Empty);
        context.CausationId.ShouldBeNull();
    }

    [Fact]
    public void TheSelfSeededId_IsStableAcrossReads()
    {
        var context = new CorrelationContext();

        context.CorrelationId.ShouldBe(context.CorrelationId);
    }

    [Fact]
    public void AdoptingAChain_TakesBothIds()
    {
        var context = new CorrelationContext();
        var correlation = Guid.CreateVersion7();
        var causation = Guid.CreateVersion7();

        context.Adopt(correlation, causation);

        context.CorrelationId.ShouldBe(correlation);
        context.CausationId.ShouldBe(causation);
    }

    // A malformed inbound header parses to Guid.Empty. Taking it would blank the chain; ignoring it
    // leaves the request traceable under an id of its own, which is the lesser loss.
    [Fact]
    public void AdoptingAnEmptyCorrelation_IsIgnored()
    {
        var context = new CorrelationContext();
        var seeded = context.CorrelationId;

        context.Adopt(Guid.Empty, causation: null);

        context.CorrelationId.ShouldBe(seeded);
    }

    [Fact]
    public void AdoptingAnEmptyCorrelation_StillRecordsTheCause()
    {
        var context = new CorrelationContext();
        var causation = Guid.CreateVersion7();

        context.Adopt(Guid.Empty, causation);

        context.CausationId.ShouldBe(causation);
    }
}
