namespace Ticketing.Api.Endpoints;

/// <summary>Request body for scanning/checking in a ticket at the gate.</summary>
/// <param name="Token">The ticket's opaque scan token, as read from its QR code.</param>
/// <param name="EventId">The event this gate is scanning for — the ticket must belong to it.</param>
/// <param name="GateId">
/// The physical gate this scanner represents, if any. Omitted for an unscoped "master" scanner
/// that bypasses any section-level gate restriction (e.g. a floor supervisor's device) — a
/// deliberate posture, not an oversight.
/// </param>
public sealed record ScanTicketRequest(string Token, Guid EventId, Guid? GateId = null);
