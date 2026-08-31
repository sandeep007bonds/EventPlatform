namespace Catalog.Application.Features.DeactivateTicketType;

/// <summary>The outcome of a deactivate-ticket-type attempt.</summary>
public enum DeactivateTicketTypeOutcome
{
    /// <summary>The type is now inactive.</summary>
    Deactivated,

    /// <summary>No such event or type, or it belongs to another tenant — deliberately indistinguishable.</summary>
    NotFound,
}
