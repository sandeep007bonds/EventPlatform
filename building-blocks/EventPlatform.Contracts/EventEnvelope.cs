namespace EventPlatform.Contracts;

/// <summary>
/// The delivery metadata that travels alongside a published <see cref="IntegrationEvent"/>, under a
/// reserved <c>envelope</c> property of the published JSON.
/// </summary>
/// <remarks>
/// <b>Beside the event, not inside it.</b> Every contract record declares
/// <see cref="IntegrationEvent"/>'s fields positionally, so widening the base type would rewrite
/// all nineteen of them and every place one is constructed — a large, risky change for plumbing no
/// domain handler reads. Keeping the envelope separate also means a consumer's typed binding is
/// untouched: <c>System.Text.Json</c> ignores properties it does not know, so a handler that binds
/// <c>EventSessionPublished</c> keeps working and only the code that wants the envelope looks for it.
/// <para>
/// It is plain JSON rather than CloudEvent extension attributes on purpose. Dapr is the broker
/// today and Service Bus or Kafka could be tomorrow (ADR-0004); a reserved property survives that
/// change, a broker-specific header convention does not.
/// </para>
/// </remarks>
/// <param name="MessageId">
/// The outbox row's id, and the CloudEvent id the relay publishes — the key a consumer dedupes on,
/// and the value a downstream event will carry as its <see cref="CausationId"/>.
/// </param>
/// <param name="CorrelationId">
/// Shared by everything descending from one originating action, across every service it touches.
/// </param>
/// <param name="CausationId">
/// The message that caused this one, or <see langword="null"/> when a person or a timer started the
/// chain.
/// </param>
/// <param name="EventType">The .NET type name of the event, for a consumer routing without binding.</param>
/// <param name="EventVersion">
/// The contract's version, from <see cref="EventVersionAttribute"/>. Lets a consumer keep handling
/// v1 while a producer moves to v2, instead of the two having to deploy together.
/// </param>
/// <param name="OccurredAt">When the event happened — copied from the event so the envelope reads alone.</param>
/// <param name="TenantId">The owning tenant, copied for the same reason.</param>
public sealed record EventEnvelope(
    Guid MessageId,
    Guid CorrelationId,
    Guid? CausationId,
    string EventType,
    int EventVersion,
    DateTimeOffset OccurredAt,
    Guid TenantId)
{
    /// <summary>The JSON property the envelope is published under.</summary>
    private const string PropertyName = "envelope";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Attaches this envelope to a serialized event, returning the node to publish.</summary>
    /// <remarks>
    /// The write side of the pair, living on the same type as <see cref="TryRead"/> for a reason:
    /// the property name is written in one place and read in one place, and if those two ever
    /// disagreed nothing would break loudly — the envelope would simply never be found, and every
    /// message would look like the start of its own chain.
    /// <para>
    /// A payload that is not a JSON object is returned unchanged rather than rejected. No record
    /// serializes that way today, but losing an envelope on one message is a gap in a trail while
    /// dropping the message is a lost sale.
    /// </para>
    /// </remarks>
    /// <param name="payload">The parsed integration event.</param>
    /// <returns>The event with the envelope attached, ready to publish.</returns>
    public JsonNode AttachTo(JsonNode? payload)
    {
        if (payload is not JsonObject payloadObject)
        {
            return payload ?? new JsonObject();
        }

        payloadObject[PropertyName] = JsonSerializer.SerializeToNode(this, SerializerOptions);
        return payloadObject;
    }

    /// <summary>Reads an envelope off a received message, if it carries one.</summary>
    /// <remarks>
    /// The read side. Returns <see langword="false"/> rather than throwing for anything malformed:
    /// a message whose envelope cannot be read is still a message that has to be handled.
    /// </remarks>
    /// <param name="body">The parsed message body.</param>
    /// <param name="envelope">The envelope, when one was present and readable.</param>
    /// <returns><see langword="true"/> if an envelope was read.</returns>
    public static bool TryRead(JsonNode? body, out EventEnvelope? envelope)
    {
        envelope = null;

        if (body?[PropertyName] is not JsonObject found)
        {
            return false;
        }

        try
        {
            envelope = found.Deserialize<EventEnvelope>(SerializerOptions);
        }
        catch (JsonException)
        {
            return false;
        }

        return envelope is not null;
    }
}
