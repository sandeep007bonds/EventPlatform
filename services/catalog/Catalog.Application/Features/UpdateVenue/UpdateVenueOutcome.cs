namespace Catalog.Application.Features.UpdateVenue;

/// <summary>Result of attempting to update a venue.</summary>
public enum UpdateVenueOutcome
{
    /// <summary>The venue was updated.</summary>
    Updated,

    /// <summary>No matching venue exists for the caller's tenant.</summary>
    NotFound,
}
