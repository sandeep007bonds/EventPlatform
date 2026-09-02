namespace Venues.Tests.Domain;

// Publish validation is the last gate before a layout becomes something tickets are sold against.
// Every rule below exists because the alternative is a ticket that names a seat nobody can find.
public sealed class SeatMapVersionTests
{
    [Fact]
    public void AValidLayout_PublishesAndReportsItsCapacity()
    {
        var version = Draft(LayoutBuilder.Simple(rows: 3, seatsPerRow: 10));

        version.Validate().ShouldBeEmpty();
        version.Capacity.ShouldBe(30);

        version.Publish(DateTimeOffset.UtcNow);

        version.Status.ShouldBe(SeatMapVersionStatus.Published);
        version.PublishedAt.ShouldNotBeNull();
    }

    // The whole reason versions exist. A published layout that could still be edited would silently
    // move seats out from under tickets already sold against it.
    [Fact]
    public void APublishedVersion_CannotBeEdited()
    {
        var version = Draft(LayoutBuilder.Simple());
        version.Publish(DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(() => version.ReplaceLayout(LayoutBuilder.Simple()));
        Should.Throw<InvalidOperationException>(() => version.Publish(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ASeatNumberRepeatedInARow_BlocksPublish()
    {
        var version = Draft(new SeatMapLayout(
            [
                new SectionDraft(
                    "LT",
                    "Lower Tier",
                    0,
                    null,
                    [
                        new SeatRowDraft(
                            "A",
                            0,
                            [
                                new SeatDraft("1", SeatAttributes.None, true),
                                new SeatDraft("1", SeatAttributes.None, true),
                            ]),
                    ]),
            ],
            [],
            []));

        version.Validate().ShouldContain(error => error.Code == "duplicate_seat_number");
    }

    [Fact]
    public void ARowLabelRepeatedInASection_BlocksPublish()
    {
        var version = Draft(new SeatMapLayout(
            [
                new SectionDraft(
                    "LT",
                    "Lower Tier",
                    0,
                    null,
                    [
                        new SeatRowDraft("A", 0, [new SeatDraft("1", SeatAttributes.None, true)]),
                        new SeatRowDraft("a", 1, [new SeatDraft("1", SeatAttributes.None, true)]),
                    ]),
            ],
            [],
            []));

        version.Validate().ShouldContain(error => error.Code == "duplicate_row_label");
    }

    // Sections and areas share one code space because consumers key off the code alone; two things
    // answering to "PIT" makes every such reference ambiguous.
    [Fact]
    public void ACodeSharedBetweenASectionAndAnArea_BlocksPublish()
    {
        var version = Draft(new SeatMapLayout(
            [LayoutBuilder.Section("PIT", rows: 1, seatsPerRow: 1)],
            [LayoutBuilder.Area("PIT", 100)],
            []));

        version.Validate().ShouldContain(error => error.Code == "duplicate_code");
    }

    [Fact]
    public void ASectionWithNoRows_BlocksPublish()
    {
        var version = Draft(new SeatMapLayout(
            [new SectionDraft("LT", "Lower Tier", 0, null, [])],
            [],
            []));

        version.Validate().ShouldContain(error => error.Code == "section_without_rows");
    }

    [Fact]
    public void AnAdmissionAreaWithNoCapacity_BlocksPublish()
    {
        var version = Draft(new SeatMapLayout([], [LayoutBuilder.Area("PIT", 0)], []));

        version.Validate().ShouldContain(error => error.Code == "invalid_area_capacity");
    }

    [Fact]
    public void ALayoutThatSellsNothing_BlocksPublish()
    {
        var version = Draft(new SeatMapLayout([], [], []));

        version.Validate().ShouldContain(error => error.Code == "empty_layout");
    }

    // A map with no plan at all is fine — a small hall needs none. A map with a plan that is
    // missing one block is not: the buyer cannot tell the hole from a sold-out section.
    [Fact]
    public void APartlyDrawnMap_BlocksPublishButAnUndrawnOneDoesNot()
    {
        var undrawn = Draft(new SeatMapLayout(
            [LayoutBuilder.Section("LT", 1, 4), LayoutBuilder.Section("UT", 1, 4, displayOrder: 1)],
            [],
            []));

        undrawn.Validate().ShouldBeEmpty();

        var halfDrawn = Draft(new SeatMapLayout(
            [LayoutBuilder.Section("LT", 1, 4), LayoutBuilder.Section("UT", 1, 4, displayOrder: 1)],
            [],
            [LayoutBuilder.SectionShape("LT")]));

        halfDrawn.Validate().ShouldContain(error => error.Code == "section_not_drawn");
    }

    [Fact]
    public void APolygonWithNoPoints_BlocksPublish()
    {
        var version = Draft(new SeatMapLayout(
            [LayoutBuilder.Section("LT", 1, 4)],
            [],
            [
                new SeatMapElementDraft(
                    SeatMapElementKind.SectionShape,
                    SeatMapElementShape.Polygon,
                    0,
                    0,
                    100,
                    50,
                    0,
                    null,
                    null,
                    null,
                    "LT",
                    null),
            ]));

        version.Validate().ShouldContain(error => error.Code == "missing_element_points");
    }

    // Elements name sections by code, so a code that is not in the layout is a request that cannot
    // be stored at all — a dangling reference, not something to discover at publish time.
    [Fact]
    public void AnElementNamingAnUnknownSection_IsRejectedOnSave()
    {
        var version = NewDraft();

        Should.Throw<InvalidOperationException>(() => version.ReplaceLayout(new SeatMapLayout(
            [LayoutBuilder.Section("LT", 1, 4)],
            [],
            [LayoutBuilder.SectionShape("NOPE")])));
    }

    [Fact]
    public void NonSellableSeats_CountTowardsNeitherCapacityNorSellableSeats()
    {
        var version = Draft(new SeatMapLayout(
            [
                new SectionDraft(
                    "LT",
                    "Lower Tier",
                    0,
                    null,
                    [
                        new SeatRowDraft(
                            "A",
                            0,
                            [
                                new SeatDraft("1", SeatAttributes.None, true),
                                new SeatDraft("2", SeatAttributes.None, false),
                            ]),
                    ]),
            ],
            [],
            []));

        version.Capacity.ShouldBe(1);
        version.Sections.Single().SeatCount.ShouldBe(2);
        version.Sections.Single().SellableSeatCount.ShouldBe(1);
    }

    [Fact]
    public void AVersionReportsEveryGateItRoutesThrough()
    {
        var gate = Guid.CreateVersion7();
        var version = Draft(new SeatMapLayout(
            [LayoutBuilder.Section("LT", 1, 4, gate)],
            [LayoutBuilder.Area("PIT", 100, gate)],
            []));

        version.ReferencedGateIds().ShouldBe(new[] { gate });
    }

    private static SeatMapVersion NewDraft() => SeatMap.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "Map").Draft!;

    private static SeatMapVersion Draft(SeatMapLayout layout)
    {
        var version = NewDraft();
        version.ReplaceLayout(layout);

        return version;
    }
}
