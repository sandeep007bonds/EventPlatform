namespace EventPlatform.Contracts.Ticketing;

/// <summary>
/// A single minted ticket on <see cref="OrderTicketsIssued"/> — either a reserved seat
/// (<see cref="SeatId"/> set) or a general-admission admission (<see cref="GeneralAdmissionAllocationId"/> set),
/// never both.
/// </summary>
/// <param name="TicketId">The issued ticket id.</param>
/// <param name="SeatId">The seat the ticket admits, if this ticket is for a reserved seat.</param>
/// <param name="GeneralAdmissionAllocationId">The allocation the ticket admits, if this ticket is general admission.</param>
/// <param name="Token">The ticket's opaque scan/redemption token.</param>
public sealed record IssuedTicketSummary(Guid TicketId, Guid? SeatId, Guid? GeneralAdmissionAllocationId, string Token);
