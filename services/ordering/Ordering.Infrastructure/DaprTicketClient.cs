namespace Ordering.Infrastructure;

/// <summary>
/// Talks to the Ticketing service (app-id <c>ticketing</c>) over Dapr service invocation, behind
/// the <see cref="ITicketClient"/> port used by the cancellation saga.
/// </summary>
internal sealed class DaprTicketClient : ITicketClient
{
    private const string TicketingAppId = "ticketing";

    /// <inheritdoc />
    public async Task<VoidTicketsClientResult> VoidTicketsAsync(Guid orderId, CancellationToken cancellationToken)
    {
        using var http = DaprClient.CreateInvokeHttpClient(TicketingAppId);
        using var response = await http.PostAsync($"v1/orders/{orderId}/tickets/void", content: null, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return new VoidTicketsClientResult(Succeeded: true, AlreadyCheckedIn: false);
        }

        // The endpoint returns 409 only for "one or more tickets already checked in" — any other
        // failure (e.g. 404, no tickets yet issued) is a generic failure the saga cannot recover
        // from on its own.
        return new VoidTicketsClientResult(
            Succeeded: false,
            AlreadyCheckedIn: response.StatusCode == HttpStatusCode.Conflict);
    }
}
