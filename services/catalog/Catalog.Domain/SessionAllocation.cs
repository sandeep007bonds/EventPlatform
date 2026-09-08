namespace Catalog.Domain;

/// <summary>
/// What one block of a venue is sold as, for one performance: a seat-map section or admission-area
/// <see cref="Code"/> bound to the <see cref="TicketType"/> it is sold under — or marked as not on
/// sale at all this night.
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
/// <para>
/// It is also the <b>overlay</b> a promoter arranges the hired venue with. A venue is a building and
/// an event is not, so the same hall is sold three different ways in a month: the upper tier closed
/// for a small show, the north stand advertised as "Golden Circle", a pit that holds 400 sold to
/// 200. None of those are facts about the building, so none of them belong in Venue — they belong
/// here, per performance, and they freeze when the performance publishes.
/// </para>
/// </remarks>
public sealed class SessionAllocation
{
    internal SessionAllocation(
        Guid id,
        Guid eventSessionId,
        string code,
        Guid? ticketTypeId,
        bool isExcluded,
        string? displayName,
        int? capacityOverride)
    {
        Id = id;
        EventSessionId = eventSessionId;
        Code = code;
        TicketTypeId = ticketTypeId;
        IsExcluded = isExcluded;
        DisplayName = displayName;
        CapacityOverride = capacityOverride;
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

    /// <summary>
    /// The <see cref="TicketType"/> this block is sold as — its name, price and rules.
    /// <see langword="null"/> only when the block is excluded, because a block nobody is selling
    /// has no price to name.
    /// </summary>
    public Guid? TicketTypeId { get; private set; }

    /// <summary>
    /// Whether this block is deliberately <b>not</b> on sale for this performance — the upper tier
    /// closed for a half-house show.
    /// </summary>
    /// <remarks>
    /// An excluded block is a decision, not an omission, and that difference is the whole reason
    /// this field exists. Publishing still demands every block be accounted for; excluding one is
    /// how the organizer accounts for it. Nothing about the excluded block reaches Inventory, so it
    /// provisions no seats and no capacity, and the buyer never sees it on sale.
    /// </remarks>
    public bool IsExcluded { get; private set; }

    /// <summary>
    /// What buyers should see this block called for this performance, or <see langword="null"/> to
    /// use the venue's own name.
    /// </summary>
    /// <remarks>
    /// The <see cref="Code"/> never changes with it. The code is what allocations, inventory,
    /// tickets and scanning bind to, so renaming has to be a display decision or it is a data
    /// migration. Because it lives on the performance it also survives time for free: a ticket sold
    /// into "Golden Circle" still reads that when next year's show calls the same block something
    /// else.
    /// </remarks>
    public string? DisplayName { get; private set; }

    /// <summary>
    /// How many to sell from this block, when that is fewer than it physically holds;
    /// <see langword="null"/> to sell all of it.
    /// </summary>
    /// <remarks>
    /// <b>Admission areas only.</b> An area's capacity is a number, so selling 200 of 400 is a
    /// complete instruction. A reserved section's capacity is seats with identity, and "sell 200 of
    /// 400" says nothing about <i>which</i> 200 — that is seat blocking, which Inventory already
    /// does per performance. Same reason an admission area is not a section full of invented seats.
    /// </remarks>
    public int? CapacityOverride { get; private set; }
}
