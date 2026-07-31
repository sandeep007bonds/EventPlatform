namespace Ordering.Domain;

/// <summary>
/// The checkout aggregate: a buyer's purchase of held seats. Drives the checkout saga
/// (create → pay → convert → confirm) and is deduped by <see cref="IdempotencyKey"/>.
/// </summary>
public sealed class Order
{
    private readonly List<OrderLine> _lines = new();

    // Parameterless ctor for EF Core materialization.
    private Order()
    {
    }

    private Order(
        Guid id,
        Guid tenantId,
        Guid userId,
        Guid catalogEventId,
        Guid holdId,
        string currency,
        string idempotencyKey)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        CatalogEventId = catalogEventId;
        HoldId = holdId;
        Currency = currency;
        IdempotencyKey = idempotencyKey;
        Status = OrderStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Unique order id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The buyer.</summary>
    public Guid UserId { get; private set; }

    /// <summary>The show/event the seats belong to.</summary>
    public Guid CatalogEventId { get; private set; }

    /// <summary>The hold this order was created from.</summary>
    public Guid HoldId { get; private set; }

    /// <summary>Current order status.</summary>
    public OrderStatus Status { get; private set; }

    /// <summary>Order total in minor currency units.</summary>
    public long TotalMinor { get; private set; }

    /// <summary>Pricing currency (ISO 4217).</summary>
    public string Currency { get; private set; } = default!;

    /// <summary>Idempotency key; unique per tenant, dedupes retried checkouts.</summary>
    public string IdempotencyKey { get; private set; } = default!;

    /// <summary>Reason the order failed, when <see cref="Status"/> is <see cref="OrderStatus.Failed"/>.</summary>
    public string? FailureReason { get; private set; }

    /// <summary>When the order was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The order lines.</summary>
    public IReadOnlyCollection<OrderLine> Lines => _lines;

    /// <summary>Creates a pending order from the given held seats.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="userId">The buyer.</param>
    /// <param name="catalogEventId">The show/event.</param>
    /// <param name="holdId">The hold being purchased.</param>
    /// <param name="currency">ISO 4217 currency code.</param>
    /// <param name="idempotencyKey">Idempotency key (unique per tenant).</param>
    /// <param name="lines">The order lines.</param>
    /// <returns>A new pending <see cref="Order"/>.</returns>
    public static Order Create(
        Guid tenantId,
        Guid userId,
        Guid catalogEventId,
        Guid holdId,
        string currency,
        string idempotencyKey,
        IEnumerable<OrderLineSpec> lines)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentNullException.ThrowIfNull(lines);

        var order = new Order(Guid.CreateVersion7(), tenantId, userId, catalogEventId, holdId, currency, idempotencyKey);
        foreach (var line in lines)
        {
            order._lines.Add(new OrderLine(order.Id, line));
        }

        if (order._lines.Count == 0)
        {
            throw new InvalidOperationException("An order must have at least one line.");
        }

        order.TotalMinor = order._lines.Sum(line => line.PriceMinor);
        return order;
    }

    /// <summary>Marks the order as awaiting payment.</summary>
    /// <exception cref="InvalidOperationException">The order is not pending.</exception>
    public void MarkAwaitingPayment()
    {
        Require(OrderStatus.Pending);
        Status = OrderStatus.AwaitingPayment;
    }

    /// <summary>Marks the order confirmed (paid and sold).</summary>
    /// <exception cref="InvalidOperationException">The order is not awaiting payment.</exception>
    public void MarkConfirmed()
    {
        Require(OrderStatus.AwaitingPayment);
        Status = OrderStatus.Confirmed;
    }

    /// <summary>Marks the order failed with a reason.</summary>
    /// <param name="reason">Why the order failed.</param>
    public void MarkFailed(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status is OrderStatus.Confirmed or OrderStatus.Refunded)
        {
            throw new InvalidOperationException($"A {Status} order cannot be failed.");
        }

        Status = OrderStatus.Failed;
        FailureReason = reason;
    }

    /// <summary>Marks a confirmed order refunded.</summary>
    /// <exception cref="InvalidOperationException">The order is not confirmed.</exception>
    public void MarkRefunded()
    {
        Require(OrderStatus.Confirmed);
        Status = OrderStatus.Refunded;
    }

    private void Require(OrderStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException($"Order {Id} is {Status}, expected {expected}.");
        }
    }
}
