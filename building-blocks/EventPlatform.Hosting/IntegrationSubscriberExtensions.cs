namespace EventPlatform.Hosting;

/// <summary>
/// Marks a Dapr pub/sub endpoint as an integration-event subscriber, so the envelope on the
/// incoming message joins this scope to the chain of work that produced it.
/// </summary>
public static class IntegrationSubscriberExtensions
{
    /// <summary>
    /// Reads the delivery envelope off the request body and adopts it into the scoped
    /// <see cref="ICorrelationContext"/> before the handler runs.
    /// </summary>
    /// <remarks>
    /// This is the hop that makes a chain a chain. Without it every subscriber starts a brand-new
    /// correlation id, so <c>EventSessionPublished → SeatSold → TicketIssued</c> reads as three
    /// unrelated stories instead of one, and the question "why did this ticket exist" has no answer.
    /// <para>
    /// The handler's own binding is untouched: the body is buffered, read once for the envelope,
    /// and rewound, so the typed parameter still deserializes normally and every existing
    /// subscriber keeps working without a line changing.
    /// </para>
    /// <para>
    /// A message without a readable envelope is still handled. It gets a fresh correlation id and
    /// no causation, exactly as if a person had started the chain — an untraceable seat sale is a
    /// gap in a record; a rejected one is a customer without their seat.
    /// </para>
    /// </remarks>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint being configured.</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    public static TBuilder WithIntegrationEnvelope<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            var correlation = http.RequestServices.GetService<CorrelationContext>();

            if (correlation is not null)
            {
                await AdoptEnvelopeAsync(http, correlation);
            }

            return await next(context);
        });

        return builder;
    }

    private static async Task AdoptEnvelopeAsync(HttpContext http, CorrelationContext correlation)
    {
        // Buffering is what lets the body be read twice. A pub/sub message is small and bounded —
        // the largest is a performance's allocation list, tens of rows — so this costs nothing that
        // matters, and the alternative is every subscriber hand-parsing its own envelope.
        http.Request.EnableBuffering();

        try
        {
            var body = await JsonNode.ParseAsync(http.Request.Body);

            if (EventEnvelope.TryRead(body, out var envelope) && envelope is not null)
            {
                // The *message* becomes this scope's causation, not the envelope's own causation
                // field: what caused the work happening here is the message that arrived, and
                // anything published while handling it should say so.
                correlation.Adopt(envelope.CorrelationId, causation: envelope.MessageId);
            }
        }
        catch (JsonException)
        {
            // Malformed JSON is the handler's problem to report, not this filter's to pre-empt.
            // Swallowing it here keeps the failure — and the response Dapr sees — where it belongs.
        }
        finally
        {
            http.Request.Body.Position = 0;
        }
    }
}
