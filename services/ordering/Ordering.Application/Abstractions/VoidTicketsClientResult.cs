namespace Ordering.Application.Abstractions;

/// <summary>Result of asking Ticketing to void an order's tickets.</summary>
/// <param name="Succeeded">Whether every ticket for the order was voided.</param>
/// <param name="AlreadyCheckedIn">
/// Whether the failure was specifically because a ticket was already checked in (as opposed to
/// some other failure) — only meaningful when <see cref="Succeeded"/> is <see langword="false"/>.
/// </param>
public sealed record VoidTicketsClientResult(bool Succeeded, bool AlreadyCheckedIn);
