namespace Venues.Application.Features.AddVenueGate;

/// <summary>What happened when a gate was added.</summary>
public enum AddVenueGateOutcome
{
    /// <summary>The gate was created.</summary>
    Added = 0,

    /// <summary>No such venue, or it belongs to another tenant.</summary>
    VenueNotFound = 1,

    /// <summary>A gate with that code already exists at this venue.</summary>
    DuplicateCode = 2,
}
