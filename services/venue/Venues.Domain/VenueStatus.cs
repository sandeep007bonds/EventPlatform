namespace Venues.Domain;

/// <summary>Lifecycle state of a <see cref="Venue"/>.</summary>
public enum VenueStatus
{
    /// <summary>Being set up. Not yet offered when an organizer picks a venue for an event.</summary>
    Draft = 0,

    /// <summary>In use. Selectable for new events.</summary>
    Active = 1,

    /// <summary>
    /// Retired. Not selectable for new events, but never deleted — events, orders and tickets
    /// already reference it, and their history has to keep making sense.
    /// </summary>
    Archived = 2,
}
