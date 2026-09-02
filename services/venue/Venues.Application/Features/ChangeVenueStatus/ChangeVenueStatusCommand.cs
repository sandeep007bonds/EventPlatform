namespace Venues.Application.Features.ChangeVenueStatus;

/// <summary>Command to activate or archive a venue.</summary>
/// <remarks>
/// One command with a target rather than two, because the two are the same decision seen from
/// opposite ends and share every guard. The API still exposes them as two verbs — <c>/activate</c>
/// and <c>/archive</c> — since that is what the caller means.
/// </remarks>
/// <param name="VenueId">The venue to change.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="Archive">
/// <see langword="true"/> to archive, <see langword="false"/> to make the venue active.
/// </param>
public sealed record ChangeVenueStatusCommand(Guid VenueId, Guid TenantId, bool Archive)
    : IRequest<ChangeVenueStatusOutcome>;
