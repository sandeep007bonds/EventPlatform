namespace Ticketing.Domain;

/// <summary>Lifecycle state of a ticket.</summary>
public enum TicketStatus
{
    /// <summary>Issued and valid for entry.</summary>
    Issued,

    /// <summary>Scanned at the gate.</summary>
    CheckedIn,

    /// <summary>Void (refunded or cancelled); not valid for entry.</summary>
    Void,
}
