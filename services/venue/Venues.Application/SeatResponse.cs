namespace Venues.Application;

/// <summary>A single seat as returned by the API.</summary>
/// <remarks>
/// No price and no availability, deliberately — see <see cref="Venues.Domain.Seat"/>. A client
/// drawing a bookable map joins this to Catalog's ticket products for price and to Inventory's
/// availability for what is still free.
/// </remarks>
/// <param name="Id">Seat id, stable for the life of the map version.</param>
/// <param name="Number">Seat number within the row.</param>
/// <param name="Attributes">Physical properties buyers need disclosed, as flag names.</param>
/// <param name="IsSellable">Whether the seat can ever be sold.</param>
public sealed record SeatResponse(Guid Id, string Number, string Attributes, bool IsSellable);
