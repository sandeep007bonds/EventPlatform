namespace Queue.Api.Endpoints;

/// <summary>Response body shared by <c>POST .../queue/join</c> and <c>GET .../queue/status</c>.</summary>
/// <param name="SessionId">The session id — echo this back on every subsequent status poll.</param>
/// <param name="Admitted">Whether the session may proceed to hold a seat.</param>
/// <param name="AdmissionToken">
/// The signed admission token to present at hold-placement time, present only when
/// <see cref="Admitted"/> is <see langword="true"/>.
/// </param>
/// <param name="Position">The current zero-based position in line, present only while waiting.</param>
/// <param name="EstimatedWaitSeconds">A rough wait estimate, present only while waiting.</param>
public sealed record QueueSessionResponse(
    Guid SessionId,
    bool Admitted,
    string? AdmissionToken,
    int? Position,
    int? EstimatedWaitSeconds);
