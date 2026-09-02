namespace Venues.Application.Features.PublishSeatMap;

/// <summary>Command to freeze the open draft and make it the live version.</summary>
/// <param name="SeatMapId">The seat-map id.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
public sealed record PublishSeatMapCommand(Guid SeatMapId, Guid TenantId) : IRequest<PublishSeatMapResult>;
