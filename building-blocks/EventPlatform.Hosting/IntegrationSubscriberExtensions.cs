namespace EventPlatform.Hosting;

/// <summary>
/// The two conventions every Dapr pub/sub subscriber in this platform must follow, applied as one
/// call so neither can be applied without the other.
/// </summary>
public static class IntegrationSubscriberExtensions
{
    /// <summary>The pub/sub component name. One broker, named the same everywhere.</summary>
    private const string PubSubName = "pubsub";

    /// <summary>
    /// Subscribes this endpoint to a topic, with a dead-letter topic for messages it cannot handle,
    /// and adopts each message's correlation chain into the handler's scope.
    /// </summary>
    /// <remarks>
    /// One call rather than two, because the two halves are only useful together and each fails
    /// silently on its own: a subscription with no dead-letter topic redelivers a poison message
    /// forever, and one that skips the envelope handles the message perfectly while quietly
    /// starting a new correlation chain. Neither throws, neither shows up in a test.
    /// <para>
    /// The dead-letter topic is per <b>service</b>, not per topic. Dapr delivers a dead letter back
    /// to the app that failed, so one topic per service means one drain endpoint per service
    /// instead of one per subscription — and the message's own envelope already says which topic it
    /// came from.
    /// </para>
    /// </remarks>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint being configured.</param>
    /// <param name="topic">The topic to subscribe to — an integration event's type name.</param>
    /// <param name="deadLetterTopic">Where this service's undeliverable messages go.</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    public static TBuilder SubscribesTo<TBuilder>(this TBuilder builder, string topic, string deadLetterTopic)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.WithTopic(new TopicOptions
        {
            PubsubName = PubSubName,
            Name = topic,
            DeadLetterTopic = deadLetterTopic,
        });

        return builder.WithIntegrationEnvelope();
    }

    /// <summary>
    /// Subscribes this endpoint to a service's own dead-letter topic, so undeliverable messages
    /// land somewhere a person can find them.
    /// </summary>
    /// <remarks>
    /// A dead-letter topic nobody reads is just a quieter silence than an infinite retry loop. This
    /// is the half that makes the other half worth having.
    /// <para>
    /// No dead-letter topic of its own, deliberately: if the drain itself cannot handle a message
    /// there is nowhere left to put it, and a chain of dead-letter topics only moves the question.
    /// A failure here is a genuine alert, which is what the retry cap will surface.
    /// </para>
    /// </remarks>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint being configured.</param>
    /// <param name="deadLetterTopic">This service's dead-letter topic.</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    public static TBuilder DrainsDeadLetters<TBuilder>(this TBuilder builder, string deadLetterTopic)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithTopic(PubSubName, deadLetterTopic);
    }

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
