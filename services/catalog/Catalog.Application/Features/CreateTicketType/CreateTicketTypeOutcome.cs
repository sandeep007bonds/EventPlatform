namespace Catalog.Application.Features.CreateTicketType;

/// <summary>The outcome of a create-ticket-type attempt.</summary>
public enum CreateTicketTypeOutcome
{
    /// <summary>The type was created.</summary>
    Created,

    /// <summary>No such event, or it belongs to another tenant — deliberately indistinguishable.</summary>
    EventNotFound,

    /// <summary>The event already has a type by that name.</summary>
    DuplicateName,
}
