namespace Catalog.Application.Features.GetPromoCodeByCode;

/// <summary>
/// Query to look up one of an event's promo codes by the string a buyer typed. Case-insensitive.
/// </summary>
/// <param name="EventId">The event the code belongs to.</param>
/// <param name="Code">The code as typed, in any case.</param>
public sealed record GetPromoCodeByCodeQuery(Guid EventId, string Code) : IRequest<PromoCodeDefinitionResponse?>;
