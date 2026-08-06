namespace Inventory.Domain;

/// <summary>
/// The Inventory system-of-record for a general-admission capacity pool — the counter-based
/// analogue of <see cref="InventoryItem"/> for a section with no individually addressable seats.
/// Postgres is the authority; Redis is the fast gate on the hot path, mirroring the per-seat
/// design exactly but operating on a remaining-capacity counter instead of a per-seat key.
/// <see cref="Version"/> is an optimistic-concurrency token, same role as <see cref="InventoryItem.Version"/>.
/// </summary>
public sealed class GeneralAdmissionAllocation
{
    // Parameterless ctor for EF Core materialization.
    private GeneralAdmissionAllocation()
    {
    }

    private GeneralAdmissionAllocation(
        Guid id,
        Guid tenantId,
        Guid eventId,
        Guid catalogSectionId,
        string priceTier,
        long priceMinor,
        int totalCapacity)
    {
        Id = id;
        TenantId = tenantId;
        EventId = eventId;
        CatalogSectionId = catalogSectionId;
        PriceTier = priceTier;
        PriceMinor = priceMinor;
        TotalCapacity = totalCapacity;
        HeldCount = 0;
        SoldCount = 0;
    }

    /// <summary>Unique allocation id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The event this section belongs to.</summary>
    public Guid EventId { get; private set; }

    /// <summary>The Catalog general-admission section id this allocation maps to (stable across services).</summary>
    public Guid CatalogSectionId { get; private set; }

    /// <summary>Price tier name (from the Catalog seat map).</summary>
    public string PriceTier { get; private set; } = default!;

    /// <summary>Price per admission in minor currency units (e.g. cents).</summary>
    public long PriceMinor { get; private set; }

    /// <summary>Total number of admissions sellable in this section.</summary>
    public int TotalCapacity { get; private set; }

    /// <summary>Number of admissions currently held (not yet sold, not yet released).</summary>
    public int HeldCount { get; private set; }

    /// <summary>Number of admissions sold.</summary>
    public int SoldCount { get; private set; }

    /// <summary>Number of admissions still available to hold.</summary>
    public int RemainingCapacity => TotalCapacity - HeldCount - SoldCount;

    /// <summary>Optimistic-concurrency token; incremented on every change.</summary>
    public int Version { get; private set; }

    /// <summary>Creates a general-admission allocation with nothing held or sold yet.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="eventId">The event.</param>
    /// <param name="catalogSectionId">The Catalog general-admission section id.</param>
    /// <param name="priceTier">Price tier name.</param>
    /// <param name="priceMinor">Price per admission in minor units.</param>
    /// <param name="totalCapacity">Total number of admissions sellable (positive).</param>
    /// <returns>A new <see cref="GeneralAdmissionAllocation"/>.</returns>
    public static GeneralAdmissionAllocation Create(
        Guid tenantId,
        Guid eventId,
        Guid catalogSectionId,
        string priceTier,
        long priceMinor,
        int totalCapacity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(priceTier);
        ArgumentOutOfRangeException.ThrowIfNegative(priceMinor);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalCapacity);

        return new GeneralAdmissionAllocation(Guid.CreateVersion7(), tenantId, eventId, catalogSectionId, priceTier, priceMinor, totalCapacity);
    }

    /// <summary>Holds <paramref name="quantity"/> admissions, if enough remain.</summary>
    /// <param name="quantity">Number of admissions to hold (positive).</param>
    /// <exception cref="InvalidOperationException">Fewer than <paramref name="quantity"/> admissions remain.</exception>
    public void Hold(int quantity)
    {
        if (quantity > RemainingCapacity)
        {
            throw new InvalidOperationException(
                $"Cannot hold {quantity} admissions in section {CatalogSectionId}; only {RemainingCapacity} remain.");
        }

        HeldCount += quantity;
        Version++;
    }

    /// <summary>Releases <paramref name="quantity"/> previously-held admissions back to available.</summary>
    /// <param name="quantity">Number of admissions to release (positive).</param>
    /// <exception cref="InvalidOperationException">Fewer than <paramref name="quantity"/> admissions are currently held.</exception>
    public void Release(int quantity)
    {
        if (quantity > HeldCount)
        {
            throw new InvalidOperationException(
                $"Cannot release {quantity} admissions in section {CatalogSectionId}; only {HeldCount} are held.");
        }

        HeldCount -= quantity;
        Version++;
    }

    /// <summary>Converts <paramref name="quantity"/> previously-held admissions to sold.</summary>
    /// <param name="quantity">Number of admissions to mark sold (positive).</param>
    /// <exception cref="InvalidOperationException">Fewer than <paramref name="quantity"/> admissions are currently held.</exception>
    public void MarkSold(int quantity)
    {
        if (quantity > HeldCount)
        {
            throw new InvalidOperationException(
                $"Cannot mark {quantity} admissions sold in section {CatalogSectionId}; only {HeldCount} are held.");
        }

        HeldCount -= quantity;
        SoldCount += quantity;
        Version++;
    }

    /// <summary>
    /// Releases <paramref name="quantity"/> previously-sold admissions back to available (a
    /// buyer-initiated cancellation/refund) — the reverse of <see cref="MarkSold"/>.
    /// </summary>
    /// <param name="quantity">Number of admissions to release (positive).</param>
    /// <exception cref="InvalidOperationException">Fewer than <paramref name="quantity"/> admissions are currently sold.</exception>
    public void ReleaseSold(int quantity)
    {
        if (quantity > SoldCount)
        {
            throw new InvalidOperationException(
                $"Cannot release {quantity} sold admissions in section {CatalogSectionId}; only {SoldCount} are sold.");
        }

        SoldCount -= quantity;
        Version++;
    }
}
