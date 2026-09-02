namespace Venues.Application.Features.ChangeVenueStatus;

/// <summary>What happened when a venue's status was changed.</summary>
public enum ChangeVenueStatusOutcome
{
    /// <summary>The status was changed.</summary>
    Changed = 0,

    /// <summary>No such venue, or it belongs to another tenant.</summary>
    NotFound = 1,

    /// <summary>The venue is archived, and archiving is one-way.</summary>
    AlreadyArchived = 2,
}
