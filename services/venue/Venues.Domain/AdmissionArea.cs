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
    internal AdmissionArea(
        Guid id,
        Guid seatMapVersionId,
        string code,
        string name,
        int capacity,
        int displayOrder,
        Guid? gateId,
        string? tierLabel)
    {
        Id = id;
        SeatMapVersionId = seatMapVersionId;
        Code = code;
        Name = name;
        Capacity = capacity;
        DisplayOrder = displayOrder;
        GateId = gateId;
        TierLabel = tierLabel;
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

    /// <summary>
    /// What this block is normally sold as — <c>Lower Tier</c>, <c>VIP</c>, <c>GA</c> — or
    /// <see langword="null"/> when the venue has no usual answer.
    /// </summary>
    /// <remarks>
    /// <b>A label, never a price (ADR-0041).</b> It says how the building is habitually carved up
    /// commercially, which is a fact about the building in the same way <see cref="Name"/> is. What
    /// that tier <i>costs</i> is a per-event decision and stays in Catalog's <c>TicketType</c>, and
    /// which block sells as which type on a given night stays in its <c>SessionAllocation</c> — an
    /// event is free to ignore this entirely.
    /// <para>
    /// Its only job is to spare an organizer re-typing the same mapping for every event at the same
    /// venue. Nothing reads it to decide anything.
    /// </para>
    /// </remarks>
    public string? TierLabel { get; private set; }
}
