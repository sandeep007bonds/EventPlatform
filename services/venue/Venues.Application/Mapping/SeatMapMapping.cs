namespace Venues.Application.Mapping;

/// <summary>Turns <see cref="SeatMap"/> aggregates into the shapes the API returns.</summary>
public static class SeatMapMapping
{
    /// <summary>Projects a seat map with one version's full layout.</summary>
    /// <param name="seatMap">The seat map.</param>
    /// <param name="version">The version to include.</param>
    /// <returns>The API representation.</returns>
    public static SeatMapResponse ToResponse(this SeatMap seatMap, SeatMapVersion version)
    {
        ArgumentNullException.ThrowIfNull(seatMap);
        ArgumentNullException.ThrowIfNull(version);

        return new SeatMapResponse(
            seatMap.Id,
            seatMap.VenueId,
            seatMap.TenantId,
            seatMap.Name,
            seatMap.PublishedVersionNumber,
            version.ToResponse());
    }

    /// <summary>Projects a seat map as a list entry, without any layout.</summary>
    /// <param name="seatMap">The seat map.</param>
    /// <returns>The summary representation.</returns>
    public static SeatMapSummaryResponse ToSummary(this SeatMap seatMap)
    {
        ArgumentNullException.ThrowIfNull(seatMap);

        return new SeatMapSummaryResponse(
            seatMap.Id,
            seatMap.VenueId,
            seatMap.Name,
            seatMap.PublishedVersionNumber,
            seatMap.Draft is not null,
            seatMap.Versions.Count);
    }

    /// <summary>Projects one version's full layout.</summary>
    /// <param name="version">The version.</param>
    /// <returns>The API representation.</returns>
    public static SeatMapVersionResponse ToResponse(this SeatMapVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        return new SeatMapVersionResponse(
            version.Id,
            version.VersionNumber,
            version.Status.ToString(),
            version.PublishedAt,
            version.Capacity,
            version.Sections
                .OrderBy(s => s.DisplayOrder)
                .Select(section => ToResponse(section))
                .ToList(),
            version.AdmissionAreas
                .OrderBy(a => a.DisplayOrder)
                .Select(a => new AdmissionAreaResponse(a.Id, a.Code, a.Name, a.Capacity, a.DisplayOrder, a.GateId))
                .ToList(),
            version.Elements
                .Select(element => ToResponse(element))
                .ToList());
    }

    private static VenueSectionResponse ToResponse(VenueSection section) =>
        new(
            section.Id,
            section.Code,
            section.Name,
            section.DisplayOrder,
            section.GateId,
            section.SellableSeatCount,
            section.Rows
                .OrderBy(r => r.DisplayOrder)
                .Select(row => new SeatRowResponse(
                    row.Id,
                    row.Label,
                    row.DisplayOrder,
                    row.Seats
                        .Select(seat => new SeatResponse(
                            seat.Id,
                            seat.Number,
                            seat.Attributes.ToString(),
                            seat.IsSellable))
                        .ToList()))
                .ToList());

    private static SeatMapElementResponse ToResponse(SeatMapElement element) =>
        new(
            element.Id,
            element.Kind.ToString(),
            element.Shape.ToString(),
            element.X,
            element.Y,
            element.Width,
            element.Height,
            element.Rotation,
            element.Label,
            element.PointsJson,
            element.StyleJson,
            element.VenueSectionId,
            element.AdmissionAreaId);
}
