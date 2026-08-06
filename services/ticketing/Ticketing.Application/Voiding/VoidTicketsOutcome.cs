namespace Ticketing.Application.Voiding;

/// <summary>Result of attempting to void every ticket for an order (a buyer-initiated cancellation/refund).</summary>
public enum VoidTicketsOutcome
{
    /// <summary>Every ticket for the order was voided (or already was — idempotent).</summary>
    Voided,

    /// <summary>No tickets exist for the order.</summary>
    NoTickets,

    /// <summary>At least one ticket was already checked in — nothing was voided (all-or-nothing).</summary>
    AlreadyCheckedIn,
}
