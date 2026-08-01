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
/// <param name="templates">The template store, for rendering the combined ticket-delivery email.</param>
/// <param name="renderer">The placeholder renderer, for rendering the combined ticket-delivery email.</param>
/// <param name="emailSender">The configured email vendor (or dev/logging fallback), for sending the combined ticket-delivery email.</param>
public sealed class IntegrationEventNotificationHandler(
    INotificationRepository notifications,
    IRecipientResolver recipients,
    ITemplateStore templates,
    ITemplateRenderer renderer,
    IEmailSender emailSender)
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

    /// <summary>
    /// Handles an <see cref="OrderTicketsIssued"/> event by sending one combined ticket-delivery
    /// email — the email already arrived on the event (captured at checkout), so this bypasses
    /// <see cref="IRecipientResolver"/> entirely (that port stays reserved for the future OTP/Identity
    /// use case) and <c>NotificationSendService</c> (it does its own separate <c>SaveChangesAsync</c>,
    /// which would leave a window between "delivery logged" and "event marked processed" where a
    /// crash could cause an at-least-once redelivery to double-send). The delivery-log row and the
    /// processed-event marker are written in one <see cref="INotificationRepository.SaveChangesAsync"/>.
    /// Falls back to recording a <see cref="DeliveryStatus.Skipped"/> row when no buyer email was
    /// provided at checkout (shouldn't happen once checkout requires it, but defensive).
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when handling is done.</returns>
    public async Task HandleOrderTicketsIssuedAsync(OrderTicketsIssued @event, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (await notifications.HasProcessedEventAsync(@event.EventId, cancellationToken))
        {
            return;
        }

        var template = string.IsNullOrWhiteSpace(@event.BuyerEmail)
            ? null
            : await templates.GetAsync(TemplateKeys.OrderTickets, cancellationToken);

        if (template is null)
        {
            notifications.AddDeliveryLog(
                DeliveryLogEntry.Skipped(@event.TenantId, NotificationChannel.Email, TemplateKeys.OrderTickets, @event.EventId));
            notifications.RecordProcessedEvent(@event.EventId, nameof(OrderTicketsIssued));
            await notifications.SaveChangesAsync(cancellationToken);
            return;
        }

        var ticketList = string.Join(
            Environment.NewLine,
            @event.Tickets.Select((ticket, index) => $"{index + 1}. Ticket {ticket.Token}"));

        var placeholders = new Dictionary<string, string>
        {
            ["order_id"] = @event.OrderId.ToString(),
            ["ticket_count"] = @event.Tickets.Count.ToString(CultureInfo.InvariantCulture),
            ["ticket_list"] = ticketList,
        };

        var subject = renderer.Render(template.Subject, placeholders);
        var body = renderer.Render(template.Body, placeholders);

        var result = await emailSender.SendAsync(@event.BuyerEmail!, subject, body, cancellationToken);

        notifications.AddDeliveryLog(result.Succeeded
            ? DeliveryLogEntry.Sent(@event.TenantId, NotificationChannel.Email, @event.BuyerEmail!, TemplateKeys.OrderTickets, emailSender.Provider, result.ProviderReference, @event.EventId)
            : DeliveryLogEntry.Failed(@event.TenantId, NotificationChannel.Email, @event.BuyerEmail!, TemplateKeys.OrderTickets, emailSender.Provider, result.FailureReason ?? "unknown", @event.EventId));
        notifications.RecordProcessedEvent(@event.EventId, nameof(OrderTicketsIssued));
        await notifications.SaveChangesAsync(cancellationToken);
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
