namespace Venues.Domain;

/// <summary>
/// Unreserved capacity within a seat-map version — a standing pit, a lawn, a beach, a terrace.
/// </summary>
/// <remarks>
/// Capacity without seat identity. Modelling this as a section full of invented seats would be a
/// lie the whole way down: those seats cannot be chosen, cannot be drawn, and cannot be scanned to
/// a place. What a buyer gets here is admission to the area, and the only number that matters is
/// how many people fit.
/// </remarks>
public sealed class AdmissionArea
{
    internal AdmissionArea(Guid id, Guid seatMapVersionId, string code, string name, int capacity, int displayOrder, Guid? gateId)
    {
        Id = id;
        SeatMapVersionId = seatMapVersionId;
        Code = code;
        Name = name;
        Capacity = capacity;
        DisplayOrder = displayOrder;
        GateId = gateId;
    }

    // Parameterless ctor for EF Core materialization.
    private AdmissionArea()
    {
    }

    /// <summary>Unique area id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>The seat-map version this area belongs to.</summary>
    public Guid SeatMapVersionId { get; private set; }

    /// <summary>
    /// Short stable code, unique within the version across sections and admission areas
    /// (e.g. <c>PIT</c>).
    /// </summary>
    public string Code { get; private set; } = default!;

    /// <summary>Display name (e.g. <c>Standing pit</c>).</summary>
    public string Name { get; private set; } = default!;

    /// <summary>How many people the area physically holds.</summary>
    public int Capacity { get; private set; }

    /// <summary>Ordering when areas are listed. Lower sorts first.</summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// The <see cref="VenueGate"/> this area is entered through, if the venue routes it to one.
    /// <see langword="null"/> means any gate.
    /// </summary>
    public Guid? GateId { get; private set; }
}
