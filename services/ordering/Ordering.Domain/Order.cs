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
        Guid eventSessionId,
        Guid holdId,
        string currency,
        string idempotencyKey,
        string? buyerEmail)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        CatalogEventId = catalogEventId;
        EventSessionId = eventSessionId;
        HoldId = holdId;
        Currency = currency;
        IdempotencyKey = idempotencyKey;
        BuyerEmail = buyerEmail;
        Status = OrderStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Unique order id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The buyer.</summary>
    public Guid UserId { get; private set; }

    /// <summary>The show/event the seats belong to — what promo codes, tax and fees come from.</summary>
    public Guid CatalogEventId { get; private set; }

    /// <summary>
    /// The performance the seats belong to. The event alone is not enough to say which night this
    /// order is for once a run has more than one (ADR-0039).
    /// </summary>
    public Guid EventSessionId { get; private set; }

    /// <summary>The hold this order was created from.</summary>
    public Guid HoldId { get; private set; }

    /// <summary>Current order status.</summary>
    public OrderStatus Status { get; private set; }

    /// <summary>
    /// What the buyer pays, in minor currency units: <see cref="SubtotalMinor"/> −
    /// <see cref="DiscountMinor"/> + <see cref="BookingFeeMinor"/> + <see cref="TaxMinor"/>. This is
    /// the amount charged.
    /// </summary>
    public long TotalMinor { get; private set; }

    /// <summary>Sum of the line prices before any discount or tax, in minor currency units.</summary>
    public long SubtotalMinor { get; private set; }

    /// <summary>Amount taken off by a promo code, in minor units. Zero when no code was applied.</summary>
    public long DiscountMinor { get; private set; }

    /// <summary>
    /// The booking fee charged, in minor units — the event's per-ticket fee times the number of
    /// admissions on the order. Zero when the event charges none. Not discountable, and not
    /// returned on a cancellation; see <see cref="RefundableMinor"/>.
    /// </summary>
    public long BookingFeeMinor { get; private set; }

    /// <summary>
    /// Tax charged, in minor units: tax on the post-discount subtotal plus tax on the booking fee.
    /// Zero for an untaxed event.
    /// </summary>
    public long TaxMinor { get; private set; }

    /// <summary>
    /// The tax rate applied, as a percentage, captured from the Catalog event at checkout time.
    /// Stored on the order rather than re-read later so a receipt always reproduces the arithmetic
    /// that was actually charged, even if the organizer changes the rate afterwards.
    /// </summary>
    public decimal? TaxRatePercent { get; private set; }

    /// <summary>The tax's display name at the time of purchase (e.g. <c>"GST 18%"</c>).</summary>
    public string? TaxLabel { get; private set; }

    /// <summary>
    /// What a full cancellation returns, in minor units: everything except the booking fee and the
    /// tax charged on that fee.
    /// </summary>
    /// <remarks>
    /// Derived rather than stored, from values that are themselves frozen at checkout — so it
    /// cannot drift out of step with the total it is subtracted from, and an organizer changing the
    /// event's fee or tax rate later cannot alter what an existing order refunds.
    /// <para>
    /// The tax being subtracted is recomputed with the same rounding the charge used, and the
    /// calculator taxes the fee separately for exactly this reason: subtracting a re-derived share
    /// of a single combined rounding could leave the buyer a minor unit short.
    /// </para>
    /// </remarks>
    public long RefundableMinor =>
        TotalMinor - BookingFeeMinor - OrderPricingCalculator.TaxOnMinor(BookingFeeMinor, TaxRatePercent);

    /// <summary>
    /// The Catalog promo code redeemed, if any. Ordering counts redemptions by this id to enforce
    /// the code's caps — Catalog defines the caps but cannot see orders.
    /// </summary>
    public Guid? PromoCodeId { get; private set; }

    /// <summary>The code as redeemed, kept for display on the order and receipt.</summary>
    public string? PromoCodeText { get; private set; }

    /// <summary>Pricing currency (ISO 4217).</summary>
    public string Currency { get; private set; } = default!;

    /// <summary>Idempotency key; unique per tenant, dedupes retried checkouts.</summary>
    public string IdempotencyKey { get; private set; } = default!;

    /// <summary>
    /// The buyer's email, provided at checkout for ticket delivery. Not derived from any token
    /// claim — a plain field the buyer supplies, since buyers don't necessarily log in with an
    /// email-carrying identity (see ADR-0021).
    /// </summary>
    public string? BuyerEmail { get; private set; }

    /// <summary>Reason the order failed, when <see cref="Status"/> is <see cref="OrderStatus.Failed"/>.</summary>
    public string? FailureReason { get; private set; }

    /// <summary>
    /// The Stripe PaymentIntent client secret, once the checkout saga has created it — the frontend
    /// mounts Payment Element against this. Set only while <see cref="OrderStatus.AwaitingPayment"/>.
    /// </summary>
    public string? PaymentClientSecret { get; private set; }

    /// <summary>When the order was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The order lines.</summary>
    public IReadOnlyCollection<OrderLine> Lines => _lines;

    /// <summary>Creates a pending order from the given held seats.</summary>
    /// <param name="id">
    /// The order's id, pre-minted by the caller (the checkout endpoint) rather than generated here —
    /// it doubles as the Dapr Workflow instance id for the checkout saga, so a webhook-driven
    /// payment-outcome subscriber can correlate straight back to the running saga with no lookup.
    /// </param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="userId">The buyer.</param>
    /// <param name="catalogEventId">The show/event.</param>
    /// <param name="eventSessionId">The performance the seats belong to.</param>
    /// <param name="holdId">The hold being purchased.</param>
    /// <param name="currency">ISO 4217 currency code.</param>
    /// <param name="idempotencyKey">Idempotency key (unique per tenant).</param>
    /// <param name="lines">The order lines.</param>
    /// <param name="buyerEmail">The buyer's email, for ticket delivery.</param>
    /// <param name="promoTerms">
    /// The redeemed promo code's terms, or <see langword="null"/> for no discount. Validity and
    /// redemption caps are checked by the caller — reaching here means the code is usable.
    /// </param>
    /// <param name="promoCodeId">The redeemed code's Catalog id, for counting redemptions.</param>
    /// <param name="promoCodeText">The code as redeemed, for display.</param>
    /// <param name="taxRatePercent">The event's tax rate as a percentage, or <see langword="null"/> when untaxed.</param>
    /// <param name="taxLabel">The tax's display name (e.g. <c>"GST 18%"</c>).</param>
    /// <param name="bookingFeePerTicketMinor">The event's per-ticket booking fee, in minor units.</param>
    /// <returns>A new pending <see cref="Order"/>.</returns>
    public static Order Create(
        Guid id,
        Guid tenantId,
        Guid userId,
        Guid catalogEventId,
        Guid eventSessionId,
        Guid holdId,
        string currency,
        string idempotencyKey,
        IEnumerable<OrderLineSpec> lines,
        string? buyerEmail = null,
        PromoCodeTerms? promoTerms = null,
        Guid? promoCodeId = null,
        string? promoCodeText = null,
        decimal? taxRatePercent = null,
        string? taxLabel = null,
        long bookingFeePerTicketMinor = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentNullException.ThrowIfNull(lines);

        var order = new Order(
            id,
            tenantId,
            userId,
            catalogEventId,
            eventSessionId,
            holdId,
            currency,
            idempotencyKey,
            buyerEmail);

        // Materialised once: the specs are needed both to build the lines and to price them, and
        // the caller may hand us a lazily-evaluated sequence.
        var lineSpecs = lines.ToList();
        foreach (var line in lineSpecs)
        {
            order._lines.Add(new OrderLine(order.Id, line));
        }

        if (order._lines.Count == 0)
        {
            throw new InvalidOperationException("An order must have at least one line.");
        }

        // The same calculator the /v1/checkout/quote preview uses, so the buyer is never quoted one
        // total and charged another.
        var pricing = OrderPricingCalculator.Calculate(
            lineSpecs, promoTerms, taxRatePercent, bookingFeePerTicketMinor);

        order.SubtotalMinor = pricing.SubtotalMinor;
        order.DiscountMinor = pricing.DiscountMinor;
        order.BookingFeeMinor = pricing.BookingFeeMinor;
        order.TaxMinor = pricing.TaxMinor;
        order.TotalMinor = pricing.TotalMinor;
        order.TaxRatePercent = taxRatePercent;
        order.TaxLabel = taxLabel;

        // Only recorded when the code actually took something off — a code that matched no line's
        // tier is worth nothing, and stamping it on the order would overstate what was redeemed
        // (and burn one of its capped redemptions for no benefit to the buyer).
        if (promoTerms is not null && pricing.DiscountMinor > 0)
        {
            order.PromoCodeId = promoCodeId;
            order.PromoCodeText = promoCodeText;
        }

        return order;
    }

    /// <summary>Marks the order as awaiting payment.</summary>
    /// <exception cref="InvalidOperationException">The order is not pending.</exception>
    public void MarkAwaitingPayment()
    {
        Require(OrderStatus.Pending);
        Status = OrderStatus.AwaitingPayment;
    }

    /// <summary>Records the Stripe PaymentIntent client secret once the checkout saga has created it.</summary>
    /// <param name="clientSecret">The PSP client secret.</param>
    /// <exception cref="InvalidOperationException">The order is not awaiting payment.</exception>
    public void RecordPaymentClientSecret(string clientSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        Require(OrderStatus.AwaitingPayment);
        PaymentClientSecret = clientSecret;
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
