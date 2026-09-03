namespace Catalog.Application;

/// <summary>
/// The outcome of a command that changes one performance.
/// </summary>
/// <remarks>
/// One shared result type across the nine session commands rather than nine near-identical
/// outcome enums. They all answer the same three questions — did you find it, was it allowed, and
/// what does it look like now — and the difference between them is only the sentence explaining a
/// refusal, which the aggregate already writes. The endpoints map
/// <see cref="SessionCommandOutcome"/> to 404/409/200 uniformly.
/// </remarks>
/// <param name="Outcome">What happened.</param>
/// <param name="Message">Why it was refused, when it was.</param>
/// <param name="Session">The performance as it now stands, when the command succeeded.</param>
public sealed record SessionCommandResult(
    SessionCommandOutcome Outcome,
    string? Message,
    EventSessionResponse? Session)
{
    /// <summary>The command succeeded.</summary>
    /// <param name="session">The performance as it now stands.</param>
    /// <returns>A success result.</returns>
    public static SessionCommandResult Ok(EventSessionResponse session) =>
        new(SessionCommandOutcome.Succeeded, null, session);

    /// <summary>
    /// The command succeeded and there is nothing left to return — the performance was removed.
    /// </summary>
    /// <returns>A success result with no performance.</returns>
    public static SessionCommandResult Removed() =>
        new(SessionCommandOutcome.Succeeded, null, null);

    /// <summary>
    /// No such event or performance — or it belongs to another tenant, which is reported the same
    /// way so an id probe cannot confirm what exists.
    /// </summary>
    /// <returns>A not-found result.</returns>
    public static SessionCommandResult NotFound() =>
        new(SessionCommandOutcome.NotFound, null, null);

    /// <summary>The command was understood but the state does not allow it.</summary>
    /// <param name="message">Why, in words the organizer can act on.</param>
    /// <returns>A refused result.</returns>
    public static SessionCommandResult Refused(string message) =>
        new(SessionCommandOutcome.Refused, message, null);
}
