namespace Communication.Domain;

/// <summary>Outcome of an attempted notification delivery.</summary>
public enum DeliveryStatus
{
    /// <summary>Handed off to the vendor successfully.</summary>
    Sent,

    /// <summary>The vendor call failed.</summary>
    Failed,

    /// <summary>Not attempted — e.g. no recipient contact info could be resolved.</summary>
    Skipped,
}
