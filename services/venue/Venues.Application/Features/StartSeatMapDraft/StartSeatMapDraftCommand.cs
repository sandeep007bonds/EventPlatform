namespace Venues.Application.Features.StartSeatMapDraft;

/// <summary>
/// Command to open a new draft version of a seat map, pre-filled with the published version's
/// layout so a structural change starts from what is live.
/// </summary>
/// <param name="SeatMapId">The seat-map id.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
public sealed record StartSeatMapDraftCommand(Guid SeatMapId, Guid TenantId) : IRequest<StartSeatMapDraftResult>;
