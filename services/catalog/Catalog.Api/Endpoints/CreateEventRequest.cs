namespace Catalog.Api.Endpoints;

/// <summary>
/// Request body for creating an event. The tenant is taken from the caller's token,
/// never from this body (ADR-0011).
/// </summary>
/// <param name="VenueId">Venue the event is held at.</param>
/// <param name="Title">Event title.</param>
/// <param name="StartsAt">Scheduled start (UTC).</param>
/// <param name="Currency">ISO 4217 currency code.</param>
public sealed record CreateEventRequest(Guid VenueId, string Title, DateTimeOffset StartsAt, string Currency);
