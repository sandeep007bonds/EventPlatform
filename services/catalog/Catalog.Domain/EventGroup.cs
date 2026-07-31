namespace Catalog.Domain;

/// <summary>
/// A thin, optional grouping of multiple <see cref="Event"/>s under one organizer-facing heading
/// — a multi-city tour, a conference roadshow, a comedy circuit. Each grouped <see cref="Event"/>
/// (a "leg") is still created, published, seat-mapped, held, checked out, and ticketed exactly
/// like any standalone event; this exists only to cluster legs for display and navigation, not
/// to change how any of them sell.
/// </summary>
public sealed class EventGroup
{
    // Parameterless ctor for EF Core materialization.
    private EventGroup()
    {
    }

    private EventGroup(Guid id, Guid tenantId, string title)
    {
        Id = id;
        TenantId = tenantId;
        Title = title;
    }

    /// <summary>Unique event-group id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Group title (e.g. "Coldplay World Tour").</summary>
    public string Title { get; private set; } = default!;

    /// <summary>Creates a new event group for the given tenant.</summary>
    /// <param name="tenantId">Owning tenant (organizer).</param>
    /// <param name="title">Group title.</param>
    /// <returns>A new <see cref="EventGroup"/>.</returns>
    public static EventGroup Create(Guid tenantId, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new EventGroup(Guid.CreateVersion7(), tenantId, title);
    }
}
