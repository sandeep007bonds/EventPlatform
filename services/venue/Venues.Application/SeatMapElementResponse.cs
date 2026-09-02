namespace Venues.Application;

/// <summary>One drawn element of a seat map as returned by the API.</summary>
/// <param name="Id">Element id.</param>
/// <param name="Kind">What this element depicts.</param>
/// <param name="Shape">The geometry it is drawn with.</param>
/// <param name="X">Left edge of the bounding box, in map space.</param>
/// <param name="Y">Top edge of the bounding box, in map space.</param>
/// <param name="Width">Width of the bounding box, in map space.</param>
/// <param name="Height">Height of the bounding box, in map space.</param>
/// <param name="Rotation">Clockwise rotation about the box's centre, in degrees.</param>
/// <param name="Label">Text drawn on or with the element, if any.</param>
/// <param name="PointsJson">Vertices for a polygon or path, as a JSON array of <c>[x, y]</c> pairs.</param>
/// <param name="StyleJson">Presentation hints as a JSON object.</param>
/// <param name="SectionId">The section this element draws, if it draws one.</param>
/// <param name="AdmissionAreaId">The admission area this element draws, if it draws one.</param>
public sealed record SeatMapElementResponse(
    Guid Id,
    string Kind,
    string Shape,
    double X,
    double Y,
    double Width,
    double Height,
    double Rotation,
    string? Label,
    string? PointsJson,
    string? StyleJson,
    Guid? SectionId,
    Guid? AdmissionAreaId);
