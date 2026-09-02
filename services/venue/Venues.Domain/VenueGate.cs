namespace Venues.Domain;

/// <summary>
/// A physical entry point into a <see cref="Venue"/> — Gate 3, the North Turnstiles, the beach-side
/// entrance.
/// </summary>
/// <remarks>
/// Physical configuration only. Which ticket may enter through which gate, and when, is an
/// event-time decision that belongs to the scanning side, not here — a gate does not know what is
/// on sale. A seat-map section may name a gate, which is the closest this service gets to access
/// policy, and even that is a routing hint rather than an entitlement.
/// </remarks>
public sealed class VenueGate
{
    internal VenueGate(Guid id, Guid venueId, string code, string name)
    {
        Id = id;
        VenueId = venueId;
        Code = code;
        Name = name;
        IsActive = true;
    }

    // Parameterless ctor for EF Core materialization.
    private VenueGate()
    {
    }

    /// <summary>Unique gate id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>The venue this gate belongs to.</summary>
    public Guid VenueId { get; private set; }

    /// <summary>Short stable code, unique within the venue (e.g. <c>G3</c>).</summary>
    public string Code { get; private set; } = default!;

    /// <summary>Display name (e.g. <c>Gate 3 — North</c>).</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Whether the gate is currently in use. Deactivated gates are kept, never deleted.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Renames the gate. The <see cref="Code"/> is stable and does not change.</summary>
    /// <param name="name">The new display name.</param>
    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>Takes the gate out of use without deleting it.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Puts a deactivated gate back into use.</summary>
    public void Reactivate() => IsActive = true;
}
