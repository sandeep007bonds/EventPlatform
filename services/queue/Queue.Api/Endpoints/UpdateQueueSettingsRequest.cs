namespace Queue.Api.Endpoints;

/// <summary>
/// Request body for <c>PUT /v1/events/{eventId}/queue/settings</c>. Deliberately has no
/// <c>Enabled</c> field — whether an event requires queueing is fixed at provisioning time from
/// <c>Event.RequiresQueue</c> and cannot be toggled here (see ADR-0026).
/// </summary>
/// <param name="AdmissionRatePerInterval">How many waiting sessions to admit every <see cref="IntervalSeconds"/>. Must be positive.</param>
/// <param name="IntervalSeconds">How often to promote waiting sessions. Must be positive.</param>
/// <param name="SessionTtlSeconds">How long a minted admission token stays valid. Must be positive.</param>
public sealed record UpdateQueueSettingsRequest(int AdmissionRatePerInterval, int IntervalSeconds, int SessionTtlSeconds);
