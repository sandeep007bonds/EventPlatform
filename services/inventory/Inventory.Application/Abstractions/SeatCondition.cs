namespace Inventory.Application.Abstractions;

/// <summary>
/// The authoritative (Postgres) condition of a seat that must be reflected as a restriction in the
/// Redis fast gate. Available seats need no key in the sparse Redis model, so they are not listed.
/// </summary>
public enum SeatCondition
{
    /// <summary>Held for a buyer, subject to the hold's remaining TTL.</summary>
    Held,

    /// <summary>Sold; permanently unavailable.</summary>
    Sold,
}
