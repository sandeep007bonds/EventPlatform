namespace Catalog.Domain;

/// <summary>
/// A named physical entry point at an <see cref="Event"/>'s location (e.g. "Gate A", "VIP
/// Entrance"). A seat-map section may restrict itself to one gate (see
/// <see cref="Seat.EntryGateId"/>/<see cref="GeneralAdmissionSection.EntryGateId"/>); a section
/// with no gate set may be entered through any gate.
/// </summary>
public sealed class EntryGate
{
    // Parameterless ctor for EF Core materialization.
    private EntryGate()
    {
    }

    private EntryGate(Guid id, Guid eventId, Guid tenantId, string name)
    {
        Id = id;
        EventId = eventId;
        TenantId = tenantId;
        Name = name;
    }

    /// <summary>Unique entry-gate id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>The event this gate belongs to.</summary>
    public Guid EventId { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gate name (e.g. "Gate A").</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Creates a new entry gate for an event.</summary>
    /// <param name="eventId">The event the gate belongs to.</param>
    /// <param name="tenantId">Owning tenant (organizer).</param>
    /// <param name="name">Gate name.</param>
    /// <returns>A new <see cref="EntryGate"/>.</returns>
    public static EntryGate Create(Guid eventId, Guid tenantId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new EntryGate(Guid.CreateVersion7(), eventId, tenantId, name);
    }
}
