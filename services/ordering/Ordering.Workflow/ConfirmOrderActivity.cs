namespace Ordering.Workflow;

/// <summary>Marks the order confirmed and enqueues <see cref="OrderConfirmed"/> via the outbox.</summary>
/// <param name="orders">The order repository.</param>
/// <param name="events">The integration-event publisher (outbox).</param>
public sealed class ConfirmOrderActivity(IOrderRepository orders, IEventPublisher events)
    : WorkflowActivity<ConfirmInput, bool>
{
    /// <inheritdoc />
    public override async Task<bool> RunAsync(WorkflowActivityContext context, ConfirmInput input)
    {
        var order = await orders.GetByIdAsync(input.OrderId, CancellationToken.None);
        if (order is null)
        {
            return false;
        }

        order.MarkConfirmed();

        events.Enqueue(new OrderConfirmed(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            input.TenantId,
            order.Id,
            input.CatalogEventId,
            input.UserId,
            order.TotalMinor,
            order.Currency,
            input.Lines));

        await orders.SaveChangesAsync(CancellationToken.None);
        return true;
    }
}
