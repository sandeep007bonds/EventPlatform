namespace Communication.Domain;

/// <summary>
/// An append-only, immutable record of one attempted notification delivery — the audit trail
/// behind every Email/SMS/WhatsApp send this service makes (or deliberately skips).
/// </summary>
public sealed class DeliveryLogEntry
{
    // Parameterless ctor for EF Core materialization.
    private DeliveryLogEntry()
    {
    }

    private DeliveryLogEntry(
        Guid tenantId,
        NotificationChannel channel,
        string? recipient,
        string? templateKey,
        DeliveryStatus status,
        string provider,
        string? providerReference,
        string? failureReason,
        Guid correlationId,
        Guid? causationId)
    {
        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        Channel = channel;
        Recipient = recipient;
        TemplateKey = templateKey;
        Status = status;
        Provider = provider;
        ProviderReference = providerReference;
        FailureReason = failureReason;
        CorrelationId = correlationId;
        CausationId = causationId;
        SentAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Unique delivery-log id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant, for per-organizer reporting.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The channel the notification was sent (or attempted) over.</summary>
    public NotificationChannel Channel { get; private set; }

    /// <summary>The recipient address/number, or <see langword="null"/> if none could be resolved.</summary>
    public string? Recipient { get; private set; }

    /// <summary>The template used (email only), or <see langword="null"/> for raw-body SMS/WhatsApp sends.</summary>
    public string? TemplateKey { get; private set; }

    /// <summary>The outcome of this attempt.</summary>
    public DeliveryStatus Status { get; private set; }

    /// <summary>The vendor that handled (or would have handled) the send, e.g. <c>"acs"</c>, <c>"twilio"</c>, <c>"dev-log"</c>.</summary>
    public string Provider { get; private set; } = default!;

    /// <summary>The vendor's own reference for this send, if any.</summary>
    public string? ProviderReference { get; private set; }

    /// <summary>Why the send failed, if <see cref="Status"/> is <see cref="DeliveryStatus.Failed"/>.</summary>
    public string? FailureReason { get; private set; }

    /// <summary>
    /// The chain of work this send belongs to — the same id the order, the payment and the ticket
    /// carry, so "which email went out for that purchase" is one query rather than a reconstruction.
    /// </summary>
    public Guid CorrelationId { get; private set; }

    /// <summary>
    /// The integration event id that directly triggered this send, if it came from a subscriber
    /// rather than the direct API.
    /// </summary>
    /// <remarks>
    /// This field was called <c>CorrelationId</c> until the platform grew a real one, and the name
    /// was wrong: it has always held the single message that caused the send, which is a causation.
    /// Two meanings under one name is how a trail becomes unreadable, so it was renamed rather than
    /// left to collide (ADR-0040).
    /// </remarks>
    public Guid? CausationId { get; private set; }

    /// <summary>When this attempt was recorded (UTC).</summary>
    public DateTimeOffset SentAt { get; private set; }

    /// <summary>Records a successful send.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="channel">The channel sent over.</param>
    /// <param name="recipient">The recipient address/number.</param>
    /// <param name="templateKey">The template used, for email.</param>
    /// <param name="provider">The vendor that sent it.</param>
    /// <param name="providerReference">The vendor's own reference for the send.</param>
    /// <param name="correlationId">The chain of work this send belongs to.</param>
    /// <param name="causationId">The triggering integration event id, if any.</param>
    /// <returns>A new <see cref="DeliveryLogEntry"/> with <see cref="DeliveryStatus.Sent"/>.</returns>
    public static DeliveryLogEntry Sent(
        Guid tenantId,
        NotificationChannel channel,
        string recipient,
        string? templateKey,
        string provider,
        string? providerReference,
        Guid correlationId,
        Guid? causationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        return new DeliveryLogEntry(tenantId, channel, recipient, templateKey, DeliveryStatus.Sent, provider, providerReference, failureReason: null, correlationId, causationId);
    }

    /// <summary>Records a failed send attempt.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="channel">The channel the send was attempted over.</param>
    /// <param name="recipient">The recipient address/number.</param>
    /// <param name="templateKey">The template used, for email.</param>
    /// <param name="provider">The vendor that attempted the send.</param>
    /// <param name="failureReason">Why the send failed.</param>
    /// <param name="correlationId">The chain of work this send belongs to.</param>
    /// <param name="causationId">The triggering integration event id, if any.</param>
    /// <returns>A new <see cref="DeliveryLogEntry"/> with <see cref="DeliveryStatus.Failed"/>.</returns>
    public static DeliveryLogEntry Failed(
        Guid tenantId,
        NotificationChannel channel,
        string recipient,
        string? templateKey,
        string provider,
        string failureReason,
        Guid correlationId,
        Guid? causationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(failureReason);

        return new DeliveryLogEntry(tenantId, channel, recipient, templateKey, DeliveryStatus.Failed, provider, providerReference: null, failureReason, correlationId, causationId);
    }

    /// <summary>Records a deliberately skipped send — e.g. no recipient contact info could be resolved.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="channel">The channel that would have been used.</param>
    /// <param name="templateKey">The template that would have been used, for email.</param>
    /// <param name="correlationId">The chain of work this send belongs to.</param>
    /// <param name="causationId">The triggering integration event id.</param>
    /// <returns>A new <see cref="DeliveryLogEntry"/> with <see cref="DeliveryStatus.Skipped"/>.</returns>
    public static DeliveryLogEntry Skipped(
        Guid tenantId,
        NotificationChannel channel,
        string? templateKey,
        Guid correlationId,
        Guid causationId)
    {
        return new DeliveryLogEntry(tenantId, channel, recipient: null, templateKey, DeliveryStatus.Skipped, provider: "none", providerReference: null, failureReason: "no recipient contact info resolved", correlationId, causationId);
    }
}
