namespace Venues.Domain;

/// <summary>
/// One drawn thing on a seat map — a section outline, the stage, a bar, a pillar, a label.
/// </summary>
/// <remarks>
/// <b>Graphics live here and nowhere else.</b> Logical identity (which seat, in which row, in which
/// section) and graphical layout (where it is on the plan and what shape it is) are separate on
/// purpose: moving a block on the plan must not change what a ticket refers to, and renumbering a
/// row must not require anyone to redraw anything.
/// <para>
/// Shapes are polygons and paths, not a grid of boxes, because the venues are not all rectangles.
/// A stadium tier, a theatre balcony and a beach club's shoreline terrace are the same problem at
/// different curvatures, and a model that only draws rectangles quietly excludes the third.
/// </para>
/// <para>
/// Coordinates are in an abstract map space, not pixels or metres. The client fits the map's
/// extent to whatever viewport it has, so a plan drawn once renders on a phone and a wall display
/// without the stored numbers meaning anything different.
/// </para>
/// </remarks>
public sealed class SeatMapElement
{
    internal SeatMapElement(
        Guid id,
        Guid seatMapVersionId,
        SeatMapElementKind kind,
        SeatMapElementShape shape,
        double x,
        double y,
        double width,
        double height,
        double rotation,
        string? label,
        string? pointsJson,
        string? styleJson,
        Guid? venueSectionId,
        Guid? admissionAreaId)
    {
        Id = id;
        SeatMapVersionId = seatMapVersionId;
        Kind = kind;
        Shape = shape;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Rotation = rotation;
        Label = label;
        PointsJson = pointsJson;
        StyleJson = styleJson;
        VenueSectionId = venueSectionId;
        AdmissionAreaId = admissionAreaId;
    }

    // Parameterless ctor for EF Core materialization.
    private SeatMapElement()
    {
    }

    /// <summary>Unique element id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>The seat-map version this element belongs to.</summary>
    public Guid SeatMapVersionId { get; private set; }

    /// <summary>What this element depicts.</summary>
    public SeatMapElementKind Kind { get; private set; }

    /// <summary>The geometry it is drawn with.</summary>
    public SeatMapElementShape Shape { get; private set; }

    /// <summary>Left edge of the element's bounding box, in map space.</summary>
    public double X { get; private set; }

    /// <summary>Top edge of the element's bounding box, in map space.</summary>
    public double Y { get; private set; }

    /// <summary>Width of the bounding box, in map space.</summary>
    public double Width { get; private set; }

    /// <summary>Height of the bounding box, in map space.</summary>
    public double Height { get; private set; }

    /// <summary>Clockwise rotation about the box's centre, in degrees.</summary>
    public double Rotation { get; private set; }

    /// <summary>Text drawn on or with the element, if any.</summary>
    public string? Label { get; private set; }

    /// <summary>
    /// Vertices for a <see cref="SeatMapElementShape.Polygon"/> or
    /// <see cref="SeatMapElementShape.Path"/>, as a JSON array of <c>[x, y]</c> pairs relative to
    /// the bounding box. <see langword="null"/> for rectangles and ellipses, which their bounds
    /// already describe.
    /// </summary>
    public string? PointsJson { get; private set; }

    /// <summary>
    /// Presentation hints (fill, stroke, opacity) as a JSON object. Opaque here on purpose: what a
    /// map <i>looks</i> like is the client's business, and giving the server an opinion about it
    /// would mean a migration every time a designer picked a different colour.
    /// </summary>
    public string? StyleJson { get; private set; }

    /// <summary>The <see cref="VenueSection"/> this element draws, if it draws one.</summary>
    public Guid? VenueSectionId { get; private set; }

    /// <summary>The <see cref="AdmissionArea"/> this element draws, if it draws one.</summary>
    public Guid? AdmissionAreaId { get; private set; }
}
