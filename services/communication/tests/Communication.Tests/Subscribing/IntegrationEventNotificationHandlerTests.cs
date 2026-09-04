namespace Communication.Tests.Subscribing;

public sealed class IntegrationEventNotificationHandlerTests
{
    private readonly INotificationRepository notifications = Substitute.For<INotificationRepository>();
    private readonly IRecipientResolver recipients = Substitute.For<IRecipientResolver>();
    private readonly ITemplateStore templates = Substitute.For<ITemplateStore>();
    private readonly ITemplateRenderer renderer = Substitute.For<ITemplateRenderer>();
    private readonly IEmailSender emailSender = Substitute.For<IEmailSender>();

    [Fact]
    public async Task HandleOrderConfirmed_AlreadyProcessed_IsNoOp()
    {
        var @event = new OrderConfirmed(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1000, "USD", []);
        notifications.HasProcessedEventAsync(@event.EventId, Arg.Any<CancellationToken>()).Returns(true);

        await CreateHandler().HandleOrderConfirmedAsync(@event, CancellationToken.None);

        await recipients.DidNotReceive().ResolveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        notifications.DidNotReceive().AddDeliveryLog(Arg.Any<DeliveryLogEntry>());
    }

    [Fact]
    public async Task HandleOrderConfirmed_NoRecipientResolved_RecordsSkippedAndMarksProcessed()
    {
        var @event = new OrderConfirmed(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1000, "USD", []);
        notifications.HasProcessedEventAsync(@event.EventId, Arg.Any<CancellationToken>()).Returns(false);
        recipients.ResolveAsync(@event.UserId, Arg.Any<CancellationToken>()).Returns((RecipientContact?)null);

        await CreateHandler().HandleOrderConfirmedAsync(@event, CancellationToken.None);

        notifications.Received(1).AddDeliveryLog(Arg.Is<DeliveryLogEntry>(e => e.Status == DeliveryStatus.Skipped && e.TemplateKey == TemplateKeys.OrderConfirmed));
        notifications.Received(1).RecordProcessedEvent(@event.EventId, nameof(OrderConfirmed));
        await notifications.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleTicketIssued_NoRecipientResolved_RecordsSkippedWithTicketIssuedTemplateKey()
    {
        var @event = new TicketIssued(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid());
        notifications.HasProcessedEventAsync(@event.EventId, Arg.Any<CancellationToken>()).Returns(false);
        recipients.ResolveAsync(@event.UserId, Arg.Any<CancellationToken>()).Returns((RecipientContact?)null);

        await CreateHandler().HandleTicketIssuedAsync(@event, CancellationToken.None);

        notifications.Received(1).AddDeliveryLog(Arg.Is<DeliveryLogEntry>(e => e.Status == DeliveryStatus.Skipped && e.TemplateKey == TemplateKeys.TicketIssued));
    }

    [Fact]
    public async Task HandleOrderTicketsIssued_AlreadyProcessed_IsNoOp()
    {
        var @event = new OrderTicketsIssued(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "buyer@example.com", []);
        notifications.HasProcessedEventAsync(@event.EventId, Arg.Any<CancellationToken>()).Returns(true);

        await CreateHandler().HandleOrderTicketsIssuedAsync(@event, CancellationToken.None);

        await templates.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        notifications.DidNotReceive().AddDeliveryLog(Arg.Any<DeliveryLogEntry>());
    }

    [Fact]
    public async Task HandleOrderTicketsIssued_NoBuyerEmail_RecordsSkippedAndMarksProcessed()
    {
        var @event = new OrderTicketsIssued(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, []);
        notifications.HasProcessedEventAsync(@event.EventId, Arg.Any<CancellationToken>()).Returns(false);

        await CreateHandler().HandleOrderTicketsIssuedAsync(@event, CancellationToken.None);

        await emailSender.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        notifications.Received(1).AddDeliveryLog(Arg.Is<DeliveryLogEntry>(e => e.Status == DeliveryStatus.Skipped && e.TemplateKey == TemplateKeys.OrderTickets));
        notifications.Received(1).RecordProcessedEvent(@event.EventId, nameof(OrderTicketsIssued));
        await notifications.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleOrderTicketsIssued_WithBuyerEmail_SendsCombinedEmailAndRecordsSent()
    {
        var tickets = new[] { new IssuedTicketSummary(Guid.NewGuid(), Guid.NewGuid(), null, "TOKEN1") };
        var @event = new OrderTicketsIssued(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "buyer@example.com", tickets);
        notifications.HasProcessedEventAsync(@event.EventId, Arg.Any<CancellationToken>()).Returns(false);
        templates.GetAsync(TemplateKeys.OrderTickets, Arg.Any<CancellationToken>())
            .Returns(new NotificationTemplate(TemplateKeys.OrderTickets, "subject template", "body template"));
        renderer.Render(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>()).Returns("rendered");
        emailSender.Provider.Returns("dev-log");
        emailSender.SendAsync("buyer@example.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SendResult(true, "ref-1", null));

        await CreateHandler().HandleOrderTicketsIssuedAsync(@event, CancellationToken.None);

        notifications.Received(1).AddDeliveryLog(Arg.Is<DeliveryLogEntry>(e =>
            e.Status == DeliveryStatus.Sent && e.TemplateKey == TemplateKeys.OrderTickets && e.Recipient == "buyer@example.com"));
        notifications.Received(1).RecordProcessedEvent(@event.EventId, nameof(OrderTicketsIssued));
        await notifications.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private IntegrationEventNotificationHandler CreateHandler() => new(notifications, recipients, templates, renderer, emailSender);
}
