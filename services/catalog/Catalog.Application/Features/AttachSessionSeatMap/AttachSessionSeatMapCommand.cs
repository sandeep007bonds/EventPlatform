namespace Catalog.Application.Features.AttachSessionSeatMap;

/// <summary>
/// Command to point a draft performance at a published Venue seat-map version.
/// </summary>
/// <param name="EventId">The event the performance belongs to.</param>
/// <param name="EventSessionId">The performance to attach a map to.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="SeatMapId">The Venue seat map.</param>
/// <param name="VersionNumber">
/// The version to pin, or <see langword="null"/> to pin whichever is published right now. Pinned
/// either way: resolving "the published one" at sale time would let a later reconfiguration move
/// the seats a sold ticket names.
/// </param>
public sealed record AttachSessionSeatMapCommand(
    Guid EventId,
    Guid EventSessionId,
    Guid TenantId,
    Guid SeatMapId,
    int? VersionNumber) : IRequest<SessionCommandResult>;
