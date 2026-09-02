namespace Venues.Application.Features.SaveSeatMapLayout;

/// <summary>Command to replace the open draft's whole layout.</summary>
/// <remarks>
/// Carries the domain's own <see cref="SeatMapLayout"/> rather than a parallel set of application
/// DTOs. The layout is pure data with no behaviour, and a second identical hierarchy of five types
/// would only ever be a place for the two to drift apart.
/// </remarks>
/// <param name="SeatMapId">The seat-map id.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="Layout">The complete layout to store.</param>
public sealed record SaveSeatMapLayoutCommand(Guid SeatMapId, Guid TenantId, SeatMapLayout Layout)
    : IRequest<SaveSeatMapLayoutResult>;
