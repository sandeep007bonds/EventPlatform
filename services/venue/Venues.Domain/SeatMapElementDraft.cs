namespace Venues.Domain;

/// <summary>One drawn element as the designer describes it, before the domain gives it an identity.</summary>
/// <remarks>
/// Links to a section or area by <b>code</b>, not id: when a whole layout is submitted at once,
/// neither has an id yet. The domain resolves the codes as it builds the version, and an element
/// naming something the layout does not contain is a validation error rather than a dangling id.
/// </remarks>
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
/// <param name="SectionCode">The section this element draws, by code, if it draws one.</param>
/// <param name="AdmissionAreaCode">The admission area this element draws, by code, if it draws one.</param>
public sealed record SeatMapElementDraft(
    SeatMapElementKind Kind,
    SeatMapElementShape Shape,
    double X,
    double Y,
    double Width,
    double Height,
    double Rotation,
    string? Label,
    string? PointsJson,
    string? StyleJson,
    string? SectionCode,
    string? AdmissionAreaCode);
