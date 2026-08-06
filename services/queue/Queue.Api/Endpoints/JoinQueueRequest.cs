namespace Queue.Api.Endpoints;

/// <summary>Request body for <c>POST /v1/events/{eventId}/queue/join</c>.</summary>
/// <param name="SessionId">
/// The client-generated session id, if resuming an existing session (e.g. after a page refresh).
/// Omitted on a first join — the server mints one.
/// </param>
public sealed record JoinQueueRequest(Guid? SessionId);
