namespace Ordering.Application.Abstractions;

/// <summary>
/// Talks to the Ticketing service (via Dapr service invocation) for the cancellation saga: void
/// every ticket for an order before its inventory is released and its payment refunded.
/// </summary>
public interface ITicketClient
{
    /// <summary>Voids every ticket for an order (compensation/cancellation).</summary>
    /// <param name="orderId">The order whose tickets should be voided.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The void result.</returns>
    Task<VoidTicketsClientResult> VoidTicketsAsync(Guid orderId, CancellationToken cancellationToken);
}
