namespace Venues.Domain;

/// <summary>One row as the designer describes it, before the domain gives it an identity.</summary>
/// <param name="Label">Row label, unique within its section.</param>
/// <param name="DisplayOrder">Front-to-back ordering within the section.</param>
/// <param name="Seats">The row's seats, in order.</param>
public sealed record SeatRowDraft(string Label, int DisplayOrder, IReadOnlyList<SeatDraft> Seats);
