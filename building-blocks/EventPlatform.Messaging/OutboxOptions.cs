namespace EventPlatform.Messaging;

/// <summary>Options controlling the transactional-outbox relay.</summary>
public sealed class OutboxOptions
{
    /// <summary>
    /// Name of the Dapr pub/sub component to publish through. Defaults to <c>pubsub</c>,
    /// matching the component in <c>platform/dapr/components</c>.
    /// </summary>
    public string PubSubName { get; set; } = "pubsub";

    /// <summary>How often the relay polls the outbox for pending messages. Defaults to two seconds.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Maximum number of messages published per poll. Defaults to 100.</summary>
    public int BatchSize { get; set; } = 100;
}
