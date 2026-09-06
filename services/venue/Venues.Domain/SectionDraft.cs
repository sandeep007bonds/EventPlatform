namespace Venues.Domain;

/// <summary>One section as the designer describes it, before the domain gives it an identity.</summary>
/// <param name="Code">Short stable code, unique within the version across sections and areas.</param>
/// <param name="Name">Display name.</param>
/// <param name="DisplayOrder">Ordering when sections are listed.</param>
/// <param name="GateId">The gate this section is entered through, or <see langword="null"/> for any.</param>
/// <param name="Rows">The section's rows, in order.</param>
/// <param name="TierLabel">
/// What this block is normally sold as — <c>Lower Tier</c>, <c>VIP</c>, <c>GA</c> — or
/// <see langword="null"/> when the venue has no usual answer. A <b>label, never a price</b>: see
/// ADR-0041.
/// </param>
public sealed record SectionDraft(
    string Code,
    string Name,
    int DisplayOrder,
    Guid? GateId,
    IReadOnlyList<SeatRowDraft> Rows,
    string? TierLabel = null);
