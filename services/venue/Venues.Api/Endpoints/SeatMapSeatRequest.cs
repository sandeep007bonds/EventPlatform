namespace Venues.Api.Endpoints;

/// <summary>One seat in a submitted layout.</summary>
/// <param name="Number">Seat number within the row (e.g. <c>12</c>, <c>12A</c>).</param>
/// <param name="Attributes">
/// Attribute names — <c>Accessible</c>, <c>Companion</c>, <c>RestrictedView</c>, <c>Aisle</c>.
/// A list of names rather than a bitmask integer: a designer submitting a stadium plan by hand
/// should not have to know that "accessible and on an aisle" is 9.
/// </param>
/// <param name="IsSellable">Whether the seat can ever be sold. Defaults to <see langword="true"/>.</param>
public sealed record SeatMapSeatRequest(
    string Number,
    IReadOnlyList<string>? Attributes = null,
    bool IsSellable = true);
