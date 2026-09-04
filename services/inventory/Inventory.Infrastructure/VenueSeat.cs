namespace Inventory.Infrastructure;

/// <summary>A seat, as far as Inventory reads it — an identity and whether it can ever be sold.</summary>
/// <param name="Id">The Venue seat id.</param>
/// <param name="IsSellable">Whether the seat can ever be sold.</param>
internal sealed record VenueSeat(Guid Id, bool IsSellable);
