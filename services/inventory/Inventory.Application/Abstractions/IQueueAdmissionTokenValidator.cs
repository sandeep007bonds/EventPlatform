namespace Inventory.Application.Abstractions;

/// <summary>
/// Verifies a Queue-service admission token locally — no call back to Queue at hold-placement
/// time (ADR-0026, mirroring ADR-0025's "propagate once, verify locally" hot-path philosophy).
/// </summary>
public interface IQueueAdmissionTokenValidator
{
    /// <summary>Checks whether a token is a valid, unexpired admission for the given event.</summary>
    /// <param name="token">The presented token, or <see langword="null"/> if none was supplied.</param>
    /// <param name="eventId">The event the hold is being placed for.</param>
    /// <param name="now">The current time (UTC).</param>
    /// <returns><see langword="true"/> if the token is present, correctly signed, matches the event, and unexpired.</returns>
    bool IsValid(string? token, Guid eventId, DateTimeOffset now);
}
