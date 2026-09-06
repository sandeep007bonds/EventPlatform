namespace Venues.Domain;

/// <summary>One admission area as the designer describes it, before the domain gives it an identity.</summary>
/// <param name="Code">Short stable code, unique within the version across sections and areas.</param>
/// <param name="Name">Display name.</param>
/// <param name="Capacity">How many people the area physically holds.</param>
/// <param name="DisplayOrder">Ordering when areas are listed.</param>
/// <param name="GateId">The gate this area is entered through, or <see langword="null"/> for any.</param>
/// <param name="TierLabel">
/// What this block is normally sold as — <c>Lower Tier</c>, <c>VIP</c>, <c>GA</c> — or
/// <see langword="null"/> when the venue has no usual answer. A <b>label, never a price</b>: see
/// ADR-0041.
/// </param>
public sealed record AdmissionAreaDraft(
    string Code,
    string Name,
    int Capacity,
    int DisplayOrder,
    Guid? GateId,
    string? TierLabel = null);
