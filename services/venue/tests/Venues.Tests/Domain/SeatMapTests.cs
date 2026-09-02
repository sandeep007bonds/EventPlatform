namespace Venues.Tests.Domain;

// The versioning rules. A seat map is the one asset in this service that other services store
// references into, so "which version is live, and what happened to the last one" has to be exact.
public sealed class SeatMapTests
{
    [Fact]
    public void ANewSeatMap_OpensWithAnEmptyVersionOne()
    {
        var map = SeatMap.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "End stage");

        map.Versions.Count.ShouldBe(1);
        map.Draft.ShouldNotBeNull();
        map.Draft!.VersionNumber.ShouldBe(1);
        map.Published.ShouldBeNull();
        map.PublishedVersionNumber.ShouldBeNull();
    }

    [Fact]
    public void PublishingTheDraft_MakesItLive()
    {
        var map = CreateMap();
        map.SaveDraftLayout(LayoutBuilder.Simple(rows: 2, seatsPerRow: 5));

        var published = map.PublishDraft(DateTimeOffset.UtcNow);

        published.VersionNumber.ShouldBe(1);
        map.PublishedVersionNumber.ShouldBe(1);
        map.Draft.ShouldBeNull();
        map.Published.ShouldBe(published);
    }

    // The bug this test exists for: publishing the draft first and *then* asking for "the published
    // version" briefly matches two versions. The previous one has to be captured before the swap.
    [Fact]
    public void PublishingASecondVersion_SupersedesTheFirst()
    {
        var map = CreateMap();
        map.SaveDraftLayout(LayoutBuilder.Simple());
        var first = map.PublishDraft(DateTimeOffset.UtcNow);

        map.StartNewDraft();
        var second = map.PublishDraft(DateTimeOffset.UtcNow);

        first.Status.ShouldBe(SeatMapVersionStatus.Superseded);
        second.VersionNumber.ShouldBe(2);
        map.PublishedVersionNumber.ShouldBe(2);
        map.Published.ShouldBe(second);
    }

    // A structural change starts from what is live, not from a blank canvas — nobody redraws a
    // stadium to move one block.
    [Fact]
    public void ANewDraft_StartsFromThePublishedLayout()
    {
        var map = CreateMap();
        map.SaveDraftLayout(LayoutBuilder.Simple(sectionCode: "LT", rows: 2, seatsPerRow: 5));
        map.PublishDraft(DateTimeOffset.UtcNow);

        var draft = map.StartNewDraft();

        draft.VersionNumber.ShouldBe(2);
        draft.Status.ShouldBe(SeatMapVersionStatus.Draft);
        draft.Sections.Single().Code.ShouldBe("LT");
        draft.Capacity.ShouldBe(10);

        // Copied, not shared: the new draft's seats are new rows, so editing them cannot reach back
        // into the version tickets were sold against.
        draft.Sections.Single().Id.ShouldNotBe(map.Published!.Sections.Single().Id);
    }

    [Fact]
    public void OnlyOneDraftCanBeOpenAtATime()
    {
        var map = CreateMap();

        Should.Throw<InvalidOperationException>(map.StartNewDraft);
    }

    [Fact]
    public void PublishingWithNoOpenDraft_IsRefused()
    {
        var map = CreateMap();
        map.SaveDraftLayout(LayoutBuilder.Simple());
        map.PublishDraft(DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(() => map.PublishDraft(DateTimeOffset.UtcNow));
    }

    // A failed publish must leave the live version live. Taking a venue's map offline because
    // somebody tried an edit that did not validate is a worse outcome than the edit not landing.
    [Fact]
    public void AFailedPublish_LeavesThePreviousVersionLive()
    {
        var map = CreateMap();
        map.SaveDraftLayout(LayoutBuilder.Simple());
        var first = map.PublishDraft(DateTimeOffset.UtcNow);

        map.StartNewDraft();
        map.SaveDraftLayout(new SeatMapLayout([], [], []));

        Should.Throw<InvalidOperationException>(() => map.PublishDraft(DateTimeOffset.UtcNow));

        first.Status.ShouldBe(SeatMapVersionStatus.Published);
        map.PublishedVersionNumber.ShouldBe(1);
    }

    [Fact]
    public void SavingALayoutWithNoOpenDraft_IsRefused()
    {
        var map = CreateMap();
        map.SaveDraftLayout(LayoutBuilder.Simple());
        map.PublishDraft(DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(() => map.SaveDraftLayout(LayoutBuilder.Simple()));
    }

    private static SeatMap CreateMap() =>
        SeatMap.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "End stage");
}
