namespace Inventory.Api.Endpoints;

/// <summary>
/// Request body for placing a hold over reserved seats and/or general-admission quantities. The
/// tenant and user are taken from the caller's token, never from this body (ADR-0011).
/// </summary>
/// <param name="EventId">The event the inventory belongs to.</param>
/// <param name="SeatIds">The seat ids to hold, if any.</param>
/// <param name="GeneralAdmissionSelections">The general-admission (allocation, quantity) pairs to hold, if any.</param>
/// <param name="QueueAdmissionToken">
/// The Queue-service admission token, if the buyer passed through the waiting room. Required only
/// when the event's settings have <c>RequiresQueue</c> set.
/// </param>
public sealed record PlaceHoldRequest(
    Guid EventId,
    IReadOnlyList<Guid>? SeatIds,
    IReadOnlyList<GeneralAdmissionSelectionRequest>? GeneralAdmissionSelections,
    string? QueueAdmissionToken = null);
