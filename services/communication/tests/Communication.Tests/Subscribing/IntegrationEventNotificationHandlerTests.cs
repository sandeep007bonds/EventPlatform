namespace Communication.Tests.Subscribing;

public sealed class IntegrationEventNotificationHandlerTests
{
    private readonly INotificationRepository notifications = Substitute.For<INotificationRepository>();
    private readonly IRecipientResolver recipients = Substitute.For<IRecipientResolver>();

    private IntegrationEventNotificationHandler CreateHandler() => new(notifications, recipients);

    [Fact]
    public async Task HandleOrderConfirmed_AlreadyProcessed_IsNoOp()
    {
        var @event = new OrderConfirmed(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1000, "USD", []);
        notifications.HasProcessedEventAsync(@event.EventId, Arg.Any<CancellationToken>()).Returns(true);

        await CreateHandler().HandleOrderConfirmedAsync(@event, CancellationToken.None);

        await recipients.DidNotReceive().ResolveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        notifications.DidNotReceive().AddDeliveryLog(Arg.Any<DeliveryLogEntry>());
    }

    [Fact]
    public async Task HandleOrderConfirmed_NoRecipientResolved_RecordsSkippedAndMarksProcessed()
    {
        var @event = new OrderConfirmed(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1000, "USD", []);
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
        var @event = new TicketIssued(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        notifications.HasProcessedEventAsync(@event.EventId, Arg.Any<CancellationToken>()).Returns(false);
        recipients.ResolveAsync(@event.UserId, Arg.Any<CancellationToken>()).Returns((RecipientContact?)null);

        await CreateHandler().HandleTicketIssuedAsync(@event, CancellationToken.None);

        notifications.Received(1).AddDeliveryLog(Arg.Is<DeliveryLogEntry>(e => e.Status == DeliveryStatus.Skipped && e.TemplateKey == TemplateKeys.TicketIssued));
    }
}
