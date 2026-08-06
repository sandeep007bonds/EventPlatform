namespace Queue.Application.Queueing;

/// <summary>Shared result shape for both joining and polling the waiting room.</summary>
/// <param name="Admitted">Whether the session may proceed to hold a seat.</param>
/// <param name="AdmissionToken">
/// The signed admission token, present only when <see cref="Admitted"/> is <see langword="true"/>.
/// </param>
/// <param name="Position">
/// The session's current zero-based position in line, present only while waiting.
/// </param>
/// <param name="EstimatedWaitSeconds">
/// A rough estimate of remaining wait time, present only while waiting — computed from the current
/// position and the event's configured pacing; a documented estimate, not a promise.
/// </param>
public sealed record QueueStatusResponse(bool Admitted, string? AdmissionToken, int? Position, int? EstimatedWaitSeconds);
