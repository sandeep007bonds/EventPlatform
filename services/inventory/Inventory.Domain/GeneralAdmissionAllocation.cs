namespace Inventory.Domain;

/// <summary>
/// The Inventory system-of-record for a general-admission capacity pool — the counter-based
/// analogue of <see cref="InventoryItem"/> for a section with no individually addressable seats.
/// Postgres is the authority; Redis is the fast gate on the hot path, mirroring the per-seat
/// design exactly but operating on a remaining-capacity counter instead of a per-seat key.
/// <see cref="Version"/> is an optimistic-concurrency token, same role as <see cref="InventoryItem.Version"/>.
/// </summary>
/// <remarks>
/// Keyed by <b>performance</b>, and unique on (performance, admission area, ticket type) — one area
/// can be sold under more than one type, and each of those is its own pool to count (ADR-0039).
/// </remarks>
public sealed class GeneralAdmissionAllocation
{
    // Parameterless ctor for EF Core materialization.
    private GeneralAdmissionAllocation()
    {
    }

    private GeneralAdmissionAllocation(
        Guid id,
        Guid tenantId,
        Guid eventSessionId,
        Guid catalogEventId,
        Guid admissionAreaId,
        Guid ticketTypeId,
        long priceMinor,
        int totalCapacity)
    {
        Id = id;
        TenantId = tenantId;
        EventSessionId = eventSessionId;
        CatalogEventId = catalogEventId;
        AdmissionAreaId = admissionAreaId;
        TicketTypeId = ticketTypeId;
        PriceMinor = priceMinor;
        TotalCapacity = totalCapacity;
        HeldCount = 0;
        SoldCount = 0;
    }

    /// <summary>Unique allocation id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The performance this pool is sellable for.</summary>
    public Guid EventSessionId { get; private set; }

    /// <summary>
    /// The event the performance belongs to. Denormalised for the same reason as on
    /// <see cref="InventoryItem"/>: the per-buyer limit is counted across the whole run.
    /// </summary>
    public Guid CatalogEventId { get; private set; }

    /// <summary>The Venue admission-area id this pool maps to (stable across services).</summary>
    public Guid AdmissionAreaId { get; private set; }

    /// <summary>The Catalog ticket type these admissions are sold as.</summary>
    public Guid TicketTypeId { get; private set; }

    /// <summary>Price per admission in minor currency units, snapshotted at publish time.</summary>
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
    /// <param name="eventSessionId">The performance.</param>
    /// <param name="catalogEventId">The event the performance belongs to.</param>
    /// <param name="admissionAreaId">The Venue admission-area id.</param>
    /// <param name="ticketTypeId">The ticket type these admissions are sold as.</param>
    /// <param name="priceMinor">Price per admission in minor units, at publish time.</param>
    /// <param name="totalCapacity">Total number of admissions sellable (positive).</param>
    /// <returns>A new <see cref="GeneralAdmissionAllocation"/>.</returns>
    public static GeneralAdmissionAllocation Create(
        Guid tenantId,
        Guid eventSessionId,
        Guid catalogEventId,
        Guid admissionAreaId,
        Guid ticketTypeId,
        long priceMinor,
        int totalCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(priceMinor);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalCapacity);

        return new GeneralAdmissionAllocation(
            Guid.CreateVersion7(),
            tenantId,
            eventSessionId,
            catalogEventId,
            admissionAreaId,
            ticketTypeId,
            priceMinor,
            totalCapacity);
    }

    /// <summary>Holds <paramref name="quantity"/> admissions, if enough remain.</summary>
    /// <param name="quantity">Number of admissions to hold (positive).</param>
    /// <exception cref="InvalidOperationException">Fewer than <paramref name="quantity"/> admissions remain.</exception>
    public void Hold(int quantity)
    {
        if (quantity > RemainingCapacity)
        {
            throw new InvalidOperationException(
                $"Cannot hold {quantity} admissions in area {AdmissionAreaId}; only {RemainingCapacity} remain.");
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
                $"Cannot release {quantity} admissions in area {AdmissionAreaId}; only {HeldCount} are held.");
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
                $"Cannot mark {quantity} admissions sold in area {AdmissionAreaId}; only {HeldCount} are held.");
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
                $"Cannot release {quantity} sold admissions in area {AdmissionAreaId}; only {SoldCount} are sold.");
        }

        SoldCount -= quantity;
        Version++;
    }
}
