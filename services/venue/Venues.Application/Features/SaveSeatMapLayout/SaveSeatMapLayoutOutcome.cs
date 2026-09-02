namespace Venues.Application.Features.SaveSeatMapLayout;

/// <summary>What happened when a draft layout was saved.</summary>
public enum SaveSeatMapLayoutOutcome
{
    /// <summary>The layout was stored.</summary>
    Saved = 0,

    /// <summary>No such seat map, or it belongs to another tenant.</summary>
    NotFound = 1,

    /// <summary>There is no open draft. Start a new version first.</summary>
    NoOpenDraft = 2,

    /// <summary>The layout is internally inconsistent — see the message.</summary>
    InvalidLayout = 3,

    /// <summary>A section or area names a gate that is not this venue's, or is not in use.</summary>
    UnknownGate = 4,
}
