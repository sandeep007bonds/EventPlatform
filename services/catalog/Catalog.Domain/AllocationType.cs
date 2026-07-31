namespace Catalog.Domain;

/// <summary>How a <see cref="SeatMap"/> section's sellable units are allocated.</summary>
public enum AllocationType
{
    /// <summary>Individually addressable seats — rows and seat numbers, generated as <see cref="Seat"/> rows.</summary>
    Reserved,

    /// <summary>A capacity-only pool with no individual seat identity — general admission.</summary>
    GeneralAdmission,
}
