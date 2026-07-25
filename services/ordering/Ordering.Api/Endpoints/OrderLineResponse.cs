namespace Ordering.Api.Endpoints;

/// <summary>Read model for a single order line.</summary>
/// <param name="SeatId">The Catalog seat id.</param>
/// <param name="PriceMinor">Price in minor currency units.</param>
public sealed record OrderLineResponse(Guid SeatId, long PriceMinor);
