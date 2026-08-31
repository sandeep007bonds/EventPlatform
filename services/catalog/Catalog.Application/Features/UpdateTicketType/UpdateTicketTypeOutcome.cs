namespace Catalog.Application.Features.UpdateTicketType;

/// <summary>The outcome of an update-ticket-type attempt.</summary>
public enum UpdateTicketTypeOutcome
{
    /// <summary>The type was updated.</summary>
    Updated,

    /// <summary>No such event or type, or it belongs to another tenant — deliberately indistinguishable.</summary>
    NotFound,

    /// <summary>Another type on the same event already has that name.</summary>
    DuplicateName,

    /// <summary>The price change was rejected because the event is no longer a draft.</summary>
    PriceLockedAfterPublish,
}
