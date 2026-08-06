namespace Ordering.Workflow;

/// <summary>Voids every ticket for an order (cancellation) in Ticketing.</summary>
/// <param name="ticketClient">The Ticketing client.</param>
public sealed class VoidTicketsActivity(ITicketClient ticketClient) : WorkflowActivity<Guid, VoidTicketsClientResult>
{
    /// <inheritdoc />
    public override Task<VoidTicketsClientResult> RunAsync(WorkflowActivityContext context, Guid orderId) =>
        ticketClient.VoidTicketsAsync(orderId, CancellationToken.None);
}
