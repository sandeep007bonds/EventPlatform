namespace Queue.Infrastructure.Admission;

/// <summary>Options for the admission controller.</summary>
public sealed class QueueAdmissionOptions
{
    /// <summary>
    /// How often the controller checks whether any event's own pacing interval has elapsed.
    /// One shared outer tick drives many independently-paced events, rather than one timer per
    /// event. Defaults to two seconds.
    /// </summary>
    public TimeSpan TickInterval { get; set; } = TimeSpan.FromSeconds(2);
}
