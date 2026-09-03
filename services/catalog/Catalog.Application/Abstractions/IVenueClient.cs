namespace Catalog.Application.Abstractions;

/// <summary>
/// Reads a seat-map version from the Venue service. Implemented in the Infrastructure layer via
/// Dapr service invocation.
/// </summary>
/// <remarks>
/// Called at exactly two moments, both of them cold: attaching a seat map to a performance, and
/// validating a publish. Nothing on the selling path calls it — a buyer loading an event page gets
/// the venue's name from the <see cref="Domain.VenueSnapshot"/> cached on the session, and the seat
/// map itself from Venue directly. Resolve live where correctness depends on it, cache where only
/// display does (ADR-0024).
/// </remarks>
public interface IVenueClient
{
    /// <summary>
    /// Gets one seat-map version, or <see langword="null"/> when the map or that version does not
    /// exist.
    /// </summary>
    /// <param name="seatMapId">The seat-map id.</param>
    /// <param name="versionNumber">
    /// The version to read, or <see langword="null"/> for whichever is currently published.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The version, or <see langword="null"/>.</returns>
    Task<SeatMapVersionSnapshot?> GetSeatMapVersionAsync(
        Guid seatMapId,
        int? versionNumber,
        CancellationToken cancellationToken);
}
