namespace Ticketing.Api.Endpoints;

/// <summary>Request body for scanning/checking in a ticket at the gate.</summary>
/// <param name="Token">The ticket's opaque scan token, as read from its QR code.</param>
public sealed record ScanTicketRequest(string Token);
