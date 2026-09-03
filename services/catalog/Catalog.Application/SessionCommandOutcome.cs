namespace Catalog.Application;

/// <summary>What happened when a command tried to change a performance.</summary>
public enum SessionCommandOutcome
{
    /// <summary>The change was made.</summary>
    Succeeded = 0,

    /// <summary>No such event or performance, or it belongs to another tenant.</summary>
    NotFound = 1,

    /// <summary>Understood, but the current state does not allow it — see the message.</summary>
    Refused = 2,
}
