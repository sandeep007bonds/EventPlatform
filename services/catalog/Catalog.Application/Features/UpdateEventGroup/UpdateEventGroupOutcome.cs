namespace Catalog.Application.Features.UpdateEventGroup;

/// <summary>Result of attempting to update an event group.</summary>
public enum UpdateEventGroupOutcome
{
    /// <summary>The event group was updated.</summary>
    Updated,

    /// <summary>No matching event group exists for the caller's tenant.</summary>
    NotFound,
}
