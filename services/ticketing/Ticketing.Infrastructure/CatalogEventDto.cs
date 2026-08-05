namespace Ticketing.Infrastructure;

/// <summary>Subset of Catalog's <c>GET /v1/events/{id}</c> response needed for a scan's time-window check.</summary>
/// <param name="StartsAt">Scheduled start time (UTC).</param>
/// <param name="EndsAt">Scheduled end time (UTC).</param>
/// <param name="DoorsOpenAt">Doors-open time (UTC), if set.</param>
internal sealed record CatalogEventDto(DateTimeOffset StartsAt, DateTimeOffset EndsAt, DateTimeOffset? DoorsOpenAt);
