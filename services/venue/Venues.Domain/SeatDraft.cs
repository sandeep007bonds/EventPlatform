namespace Venues.Domain;

/// <summary>One seat as the designer describes it, before the domain gives it an identity.</summary>
/// <param name="Number">Seat number within the row.</param>
/// <param name="Attributes">Physical properties buyers need disclosed.</param>
/// <param name="IsSellable">Whether the seat can ever be sold.</param>
public sealed record SeatDraft(string Number, SeatAttributes Attributes, bool IsSellable);
