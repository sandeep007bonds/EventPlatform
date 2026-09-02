namespace Venues.Domain;

/// <summary>Lifecycle state of a <see cref="SeatMapVersion"/>.</summary>
public enum SeatMapVersionStatus
{
    /// <summary>Being edited. The only state in which the layout can change.</summary>
    Draft = 0,

    /// <summary>
    /// Published and immutable. Events point at a published version, and tickets sold against it
    /// name seats that must keep meaning the same place.
    /// </summary>
    Published = 1,

    /// <summary>
    /// Was published, and a later version has replaced it. Kept, not deleted: tickets sold against
    /// it are still valid and still have to resolve.
    /// </summary>
    Superseded = 2,
}
