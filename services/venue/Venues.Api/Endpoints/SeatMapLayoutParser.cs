namespace Venues.Api.Endpoints;

/// <summary>
/// Turns a submitted <see cref="SaveSeatMapLayoutRequest"/> into the domain's
/// <see cref="SeatMapLayout"/>.
/// </summary>
/// <remarks>
/// The wire format spells enums as names — <c>"Polygon"</c>, <c>"Accessible"</c> — because a
/// designer reading or writing a plan by hand should not have to know the numbers, and because a
/// number silently becomes the wrong member the day one is inserted. That means an unrecognised
/// name is a real possibility, and it is reported as a 400 naming the offending value rather than
/// being coerced to whatever member happens to be zero.
/// </remarks>
public static class SeatMapLayoutParser
{
    /// <summary>Parses a submitted layout.</summary>
    /// <param name="request">The submitted layout.</param>
    /// <returns>The parsed layout, or the first name that was not recognised.</returns>
    public static SeatMapLayoutParseResult Parse(SaveSeatMapLayoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sections = new List<SectionDraft>();
        foreach (var section in request.Sections ?? [])
        {
            var rows = new List<SeatRowDraft>();
            foreach (var row in section.Rows ?? [])
            {
                var seats = new List<SeatDraft>();
                foreach (var seat in row.Seats ?? [])
                {
                    if (!TryParseAttributes(seat.Attributes, out var attributes, out var badAttribute))
                    {
                        return new SeatMapLayoutParseResult(null, $"'{badAttribute}' is not a seat attribute.");
                    }

                    seats.Add(new SeatDraft(seat.Number, attributes, seat.IsSellable));
                }

                rows.Add(new SeatRowDraft(row.Label, row.DisplayOrder, seats));
            }

            sections.Add(new SectionDraft(section.Code, section.Name, section.DisplayOrder, section.GateId, rows));
        }

        var areas = (request.AdmissionAreas ?? [])
            .Select(area => new AdmissionAreaDraft(
                area.Code,
                area.Name,
                area.Capacity,
                area.DisplayOrder,
                area.GateId))
            .ToList();

        var elements = new List<SeatMapElementDraft>();
        foreach (var element in request.Elements ?? [])
        {
            if (!Enum.TryParse<SeatMapElementKind>(element.Kind, ignoreCase: true, out var kind)
                || !Enum.IsDefined(kind))
            {
                return new SeatMapLayoutParseResult(null, $"'{element.Kind}' is not a seat-map element kind.");
            }

            if (!Enum.TryParse<SeatMapElementShape>(element.Shape, ignoreCase: true, out var shape)
                || !Enum.IsDefined(shape))
            {
                return new SeatMapLayoutParseResult(null, $"'{element.Shape}' is not a seat-map element shape.");
            }

            elements.Add(new SeatMapElementDraft(
                kind,
                shape,
                element.X,
                element.Y,
                element.Width,
                element.Height,
                element.Rotation,
                element.Label,
                element.PointsJson,
                element.StyleJson,
                element.SectionCode,
                element.AdmissionAreaCode));
        }

        return new SeatMapLayoutParseResult(new SeatMapLayout(sections, areas, elements), null);
    }

    private static bool TryParseAttributes(
        IReadOnlyList<string>? names,
        out SeatAttributes attributes,
        out string? unrecognised)
    {
        attributes = SeatAttributes.None;
        unrecognised = null;

        foreach (var name in names ?? [])
        {
            if (!Enum.TryParse<SeatAttributes>(name, ignoreCase: true, out var parsed))
            {
                unrecognised = name;
                return false;
            }

            attributes |= parsed;
        }

        return true;
    }
}
