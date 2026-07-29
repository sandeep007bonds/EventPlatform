namespace Communication.Application.Subscribing;

/// <summary>
/// Handles the two integration events Communication subscribes to (<c>OrderConfirmed</c>,
/// <c>TicketIssued</c>). Both share identical dedup-then-resolve-then-skip-or-send plumbing,
/// differing only in the intended channel/template. Recipient resolution always fails today (no
/// Identity/user-profile service exists yet — see <see cref="IRecipientResolver"/>), so every
/// delivery is currently recorded as <see cref="DeliveryStatus.Skipped"/> rather than attempted.
/// This upgrades to real delivery with no changes here once a real <see cref="IRecipientResolver"/>
/// implementation exists.
/// </summary>
/// <param name="notifications">The delivery-log/dedup-ledger repository.</param>
/// <param name="recipients">The recipient resolver.</param>
public sealed class IntegrationEventNotificationHandler(INotificationRepository notifications, IRecipientResolver recipients)
{
    /// <summary>Handles an <see cref="OrderConfirmed"/> event.</summary>
    /// <param name="event">The event.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when handling is done.</returns>
    public Task HandleOrderConfirmedAsync(OrderConfirmed @event, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);

        return HandleAsync(@event.EventId, nameof(OrderConfirmed), @event.TenantId, @event.UserId, TemplateKeys.OrderConfirmed, cancellationToken);
    }

    /// <summary>Handles a <see cref="TicketIssued"/> event.</summary>
    /// <param name="event">The event.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when handling is done.</returns>
    public Task HandleTicketIssuedAsync(TicketIssued @event, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);

        return HandleAsync(@event.EventId, nameof(TicketIssued), @event.TenantId, @event.UserId, TemplateKeys.TicketIssued, cancellationToken);
    }

    private async Task HandleAsync(Guid eventId, string eventType, Guid tenantId, Guid userId, string intendedTemplateKey, CancellationToken cancellationToken)
    {
        if (await notifications.HasProcessedEventAsync(eventId, cancellationToken))
        {
            return;
        }

        // Always null today (no IRecipientResolver implementation resolves a real contact yet), so
        // this always falls through to recording a Skipped row rather than attempting a send. Once
        // a real resolver exists, branch on a non-null contact here and call NotificationSendService
        // instead — no other change needed.
        _ = await recipients.ResolveAsync(userId, cancellationToken);

        notifications.AddDeliveryLog(DeliveryLogEntry.Skipped(tenantId, NotificationChannel.Email, intendedTemplateKey, eventId));
        notifications.RecordProcessedEvent(eventId, eventType);
        await notifications.SaveChangesAsync(cancellationToken);
    }
}
