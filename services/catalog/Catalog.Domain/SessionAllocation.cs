namespace Catalog.Domain;

/// <summary>
/// What one block of a venue is sold as, for one performance: a seat-map section or admission-area
/// <see cref="Code"/> bound to the <see cref="TicketType"/> it is sold under.
/// </summary>
/// <remarks>
/// This is where the commercial decision lives now that seats do not carry one. A Venue seat has no
/// price — deliberately, ADR-0038 — because a seat is a fact about a building and a price is a
/// decision that changes weekly. Something still has to say "Lower Tier is Gold", and it has to say
/// it <b>per performance</b>: Friday's Lower Tier can be Gold while Saturday's matinee sells the
/// same seats as Premium.
/// <para>
/// It binds by <b>code</b>, not by seat id. A section's code is stable across renames by design, and
/// binding a whole section in one row means a 60,000-seat stadium needs about twenty of these
/// instead of sixty thousand.
/// </para>
/// </remarks>
public sealed class SessionAllocation
{
    internal SessionAllocation(Guid id, Guid eventSessionId, string code, Guid ticketTypeId)
    {
        Id = id;
        EventSessionId = eventSessionId;
        Code = code;
        TicketTypeId = ticketTypeId;
    }

    // Parameterless ctor for EF Core materialization.
    private SessionAllocation()
    {
    }

    /// <summary>Unique allocation id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>The performance this allocation applies to.</summary>
    public Guid EventSessionId { get; private set; }

    /// <summary>
    /// The Venue seat-map section or admission-area code this covers (e.g. <c>LT</c>, <c>PIT</c>).
    /// Unique within the session across both kinds, because the Venue map keeps them in one code
    /// space for exactly this reason.
    /// </summary>
    public string Code { get; private set; } = default!;

    /// <summary>The <see cref="TicketType"/> this block is sold as — its name, price and rules.</summary>
    public Guid TicketTypeId { get; private set; }
}
