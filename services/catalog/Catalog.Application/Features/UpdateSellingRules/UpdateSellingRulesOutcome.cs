namespace Catalog.Application.Features.UpdateSellingRules;

/// <summary>What happened when an event's selling rules were updated.</summary>
public enum UpdateSellingRulesOutcome
{
    /// <summary>The rules were updated and an <c>EventUpdated</c> event enqueued.</summary>
    Updated,

    /// <summary>No matching event exists.</summary>
    NotFound,

    /// <summary>The event is not a draft, so its selling rules cannot be changed.</summary>
    NotDraft,

    /// <summary>The rules contradict something — see the message.</summary>
    Refused,
}
