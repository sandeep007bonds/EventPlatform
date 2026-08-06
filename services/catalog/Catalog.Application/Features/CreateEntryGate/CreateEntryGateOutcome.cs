namespace Catalog.Application.Features.CreateEntryGate;

/// <summary>Result of attempting to define an entry gate for an event.</summary>
public enum CreateEntryGateOutcome
{
    /// <summary>The entry gate was created.</summary>
    Created,

    /// <summary>No matching event exists for the caller's tenant.</summary>
    EventNotFound,
}
