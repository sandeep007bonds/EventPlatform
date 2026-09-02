namespace Venues.Api.Endpoints;

/// <summary>One drawn element in a submitted layout.</summary>
/// <param name="Kind">
/// What this element depicts — <c>SectionShape</c>, <c>AreaShape</c>, <c>Stage</c>,
/// <c>Entrance</c>, <c>Facility</c>, <c>Obstruction</c>, <c>Label</c>.
/// </param>
/// <param name="Shape">The geometry — <c>Rectangle</c>, <c>Ellipse</c>, <c>Polygon</c>, <c>Path</c>.</param>
/// <param name="X">Left edge of the bounding box, in map space.</param>
/// <param name="Y">Top edge of the bounding box, in map space.</param>
/// <param name="Width">Width of the bounding box, in map space.</param>
/// <param name="Height">Height of the bounding box, in map space.</param>
/// <param name="Rotation">Clockwise rotation about the box's centre, in degrees.</param>
/// <param name="Label">Text drawn on or with the element, if any.</param>
/// <param name="PointsJson">Vertices for a polygon or path, as a JSON array of <c>[x, y]</c> pairs.</param>
/// <param name="StyleJson">Presentation hints as a JSON object.</param>
/// <param name="SectionCode">The section this element draws, by code, if it draws one.</param>
/// <param name="AdmissionAreaCode">The admission area this element draws, by code, if it draws one.</param>
public sealed record SeatMapElementRequest(
    string Kind,
    string Shape,
    double X,
    double Y,
    double Width,
    double Height,
    double Rotation = 0,
    string? Label = null,
    string? PointsJson = null,
    string? StyleJson = null,
    string? SectionCode = null,
    string? AdmissionAreaCode = null);
