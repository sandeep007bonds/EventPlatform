namespace Catalog.Application.Features.GetEvent;

/// <summary>Read model returned for a single event.</summary>
/// <param name="Id">Event id.</param>
/// <param name="Title">Event title.</param>
/// <param name="StartsAt">Scheduled start (UTC).</param>
/// <param name="Status">Lifecycle status name.</param>
/// <param name="Currency">ISO 4217 currency code.</param>
public sealed record EventResponse(
    Guid Id,
    string Title,
    DateTimeOffset StartsAt,
    string Status,
    string Currency);
