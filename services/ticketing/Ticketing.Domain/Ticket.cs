namespace Ticketing.Domain;

/// <summary>
/// A ticket admitting one unit for one order — either a reserved seat (<see cref="SeatId"/> set) or
/// one general-admission quantity (<see cref="GeneralAdmissionAllocationId"/> set), never both.
/// Carries an opaque <see cref="Token"/> that the QR code encodes and the gate scans.
/// </summary>
public sealed class Ticket
{
    // Parameterless ctor for EF Core materialization.
    private Ticket()
    {
    }

    private Ticket(
        Guid id,
        Guid tenantId,
        Guid orderId,
        Guid catalogEventId,
        Guid? seatId,
        Guid? generalAdmissionAllocationId,
        Guid userId,
        string token)
    {
        Id = id;
        TenantId = tenantId;
        OrderId = orderId;
        CatalogEventId = catalogEventId;
        SeatId = seatId;
        GeneralAdmissionAllocationId = generalAdmissionAllocationId;
        UserId = userId;
        Token = token;
        Status = TicketStatus.Issued;
        IssuedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Unique ticket id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The order the ticket was issued for.</summary>
    public Guid OrderId { get; private set; }

    /// <summary>The show/event the seat belongs to.</summary>
    public Guid CatalogEventId { get; private set; }

    /// <summary>The seat the ticket admits, if this ticket is for a reserved seat.</summary>
    public Guid? SeatId { get; private set; }

    /// <summary>The general-admission allocation the ticket admits, if this ticket is general admission.</summary>
    public Guid? GeneralAdmissionAllocationId { get; private set; }

    /// <summary>The ticket holder.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Opaque scan token encoded in the QR code.</summary>
    public string Token { get; private set; } = default!;

    /// <summary>Current ticket status.</summary>
    public TicketStatus Status { get; private set; }

    /// <summary>When the ticket was issued (UTC).</summary>
    public DateTimeOffset IssuedAt { get; private set; }

    /// <summary>When the ticket was scanned/checked in at the gate (UTC), if it has been.</summary>
    public DateTimeOffset? CheckedInAt { get; private set; }

    /// <summary>Issues a ticket for a reserved seat or a general-admission quantity.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="orderId">The order.</param>
    /// <param name="catalogEventId">The show/event.</param>
    /// <param name="seatId">The seat admitted, if this ticket is for a reserved seat.</param>
    /// <param name="generalAdmissionAllocationId">The allocation admitted, if this ticket is general admission.</param>
    /// <param name="userId">The ticket holder.</param>
    /// <param name="token">The opaque scan token.</param>
    /// <returns>A new issued <see cref="Ticket"/>.</returns>
    /// <exception cref="ArgumentException">Neither or both of <paramref name="seatId"/>/<paramref name="generalAdmissionAllocationId"/> are set.</exception>
    public static Ticket Create(
        Guid tenantId,
        Guid orderId,
        Guid catalogEventId,
        Guid? seatId,
        Guid? generalAdmissionAllocationId,
        Guid userId,
        string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        if (seatId is null == generalAdmissionAllocationId is null)
        {
            throw new ArgumentException(
                "A ticket must admit exactly one of a seat or a general-admission allocation.");
        }

        return new Ticket(Guid.CreateVersion7(), tenantId, orderId, catalogEventId, seatId, generalAdmissionAllocationId, userId, token);
    }

    /// <summary>Marks the ticket checked in at the gate.</summary>
    /// <exception cref="InvalidOperationException">The ticket is not in the issued state.</exception>
    public void CheckIn()
    {
        if (Status != TicketStatus.Issued)
        {
            throw new InvalidOperationException($"Ticket {Id} is {Status}, not Issued.");
        }

        Status = TicketStatus.CheckedIn;
        CheckedInAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Voids the ticket (e.g. after a refund).</summary>
    public void Void()
    {
        if (Status == TicketStatus.Void)
        {
            return;
        }

        Status = TicketStatus.Void;
    }
}
