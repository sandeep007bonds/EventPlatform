namespace Venues.Application.Features.StartSeatMapDraft;

/// <summary>What happened when a new draft was requested.</summary>
public enum StartSeatMapDraftOutcome
{
    /// <summary>A new draft version was opened.</summary>
    Started = 0,

    /// <summary>No such seat map, or it belongs to another tenant.</summary>
    NotFound = 1,

    /// <summary>A draft is already open. Edit or publish that one.</summary>
    DraftAlreadyOpen = 2,
}
