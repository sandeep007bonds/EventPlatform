namespace Ticketing.Application.Issuing;

/// <summary>Outcome of issuing tickets for an order.</summary>
/// <param name="Issued">
/// <see langword="true"/> if tickets were issued; <see langword="false"/> if the order was already
/// ticketed (idempotent no-op).
/// </param>
/// <param name="TicketCount">Number of tickets issued (0 when already ticketed).</param>
public sealed record IssueResult(bool Issued, int TicketCount);
