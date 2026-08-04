namespace Identity.Domain;

/// <summary>
/// An organizer's organization. Created via self-service registration (ADR-0023) — the
/// registering organizer becomes this tenant's sole <see cref="OrganizerAccount"/>; inviting
/// additional teammates into an existing tenant is explicitly deferred, not built yet.
/// </summary>
public sealed class Tenant
{
    private Tenant()
    {
    }

    private Tenant(string name)
    {
        Id = Guid.CreateVersion7();
        Name = name;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>The tenant's stable id — this is the <c>tenant_id</c> claim stamped on organizer tokens.</summary>
    public Guid Id { get; private set; }

    /// <summary>The organization's display name.</summary>
    public string Name { get; private set; } = default!;

    /// <summary>When this tenant was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates a new tenant.</summary>
    /// <param name="name">The organization's display name.</param>
    /// <returns>A new <see cref="Tenant"/>.</returns>
    public static Tenant Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Tenant(name);
    }
}
