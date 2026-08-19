namespace Catalog.Application.Features.ListPublicPromoCodes;

/// <summary>
/// Query to list an event's *advertised* promo codes — the ones an organizer marked public and
/// that are redeemable right now. Anonymous: a buyer picking seats has not necessarily logged in
/// yet, and these codes are advertised by design.
/// </summary>
/// <param name="EventId">The event id.</param>
public sealed record ListPublicPromoCodesQuery(Guid EventId) : IRequest<IReadOnlyList<PublicPromoCodeResponse>>;
