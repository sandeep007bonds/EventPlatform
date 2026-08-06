namespace Queue.Api.Endpoints;

/// <summary>Response body for <c>GET /v1/events/{eventId}/queue/settings</c>.</summary>
/// <param name="EventId">The event id.</param>
/// <param name="Enabled">
/// Whether queueing is on for this event — fixed at provisioning time from
/// <c>Event.RequiresQueue</c>; not editable via <c>PUT</c>.
/// </param>
/// <param name="AdmissionRatePerInterval">How many waiting sessions are admitted every <see cref="IntervalSeconds"/>.</param>
/// <param name="IntervalSeconds">How often the admission controller promotes waiting sessions.</param>
/// <param name="SessionTtlSeconds">How long an admission token stays valid once minted.</param>
public sealed record QueueSettingsResponse(
    Guid EventId,
    bool Enabled,
    int AdmissionRatePerInterval,
    int IntervalSeconds,
    int SessionTtlSeconds);
