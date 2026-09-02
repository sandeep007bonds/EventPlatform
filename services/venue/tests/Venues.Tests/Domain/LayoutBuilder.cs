namespace Venues.Tests.Domain;

// Layouts are the fixture for nearly every seat-map test, and spelling one out inline buries the
// one thing a test is actually about under thirty lines of scaffolding.
internal static class LayoutBuilder
{
    public static SeatMapLayout Simple(
        string sectionCode = "LT",
        int rows = 2,
        int seatsPerRow = 3,
        Guid? gateId = null) =>
        new(
            [Section(sectionCode, rows, seatsPerRow, gateId)],
            [],
            [SectionShape(sectionCode)]);

    public static SectionDraft Section(
        string code,
        int rows,
        int seatsPerRow,
        Guid? gateId = null,
        int displayOrder = 0) =>
        new(
            code,
            $"Section {code}",
            displayOrder,
            gateId,
            Enumerable.Range(0, rows)
                .Select(r => new SeatRowDraft(
                    ((char)('A' + r)).ToString(),
                    r,
                    Enumerable.Range(1, seatsPerRow)
                        .Select(n => new SeatDraft(n.ToString(CultureInfo.InvariantCulture), SeatAttributes.None, true))
                        .ToList()))
                .ToList());

    public static AdmissionAreaDraft Area(string code, int capacity, Guid? gateId = null) =>
        new(code, $"Area {code}", capacity, 0, gateId);

    public static SeatMapElementDraft SectionShape(string sectionCode) =>
        new(
            SeatMapElementKind.SectionShape,
            SeatMapElementShape.Rectangle,
            0,
            0,
            100,
            50,
            0,
            null,
            null,
            null,
            sectionCode,
            null);

    public static SeatMapElementDraft AreaShape(string areaCode) =>
        new(
            SeatMapElementKind.AreaShape,
            SeatMapElementShape.Rectangle,
            0,
            60,
            100,
            40,
            0,
            null,
            null,
            null,
            null,
            areaCode);
}
