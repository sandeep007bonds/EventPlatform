namespace Catalog.Application.Features.CreatePromoCode;

/// <summary>
/// Command to create a discount code for an event. <see cref="TenantId"/> is set server-side from
/// the validated JWT (never from the request body), per ADR-0011.
/// </summary>
/// <param name="EventId">The event the code discounts.</param>
/// <param name="TenantId">The caller's tenant id; must own the event.</param>
/// <param name="Code">The code buyers type. Stored upper-invariant.</param>
/// <param name="Description">Organizer-facing note on what the code is for.</param>
/// <param name="DiscountType">Whether <paramref name="DiscountValue"/> is a percentage or a flat amount.</param>
/// <param name="DiscountValue">Percentage in (0, 100], or a flat amount in major currency units.</param>
/// <param name="ValidFrom">Earliest redeemable instant; <see langword="null"/> for no lower bound.</param>
/// <param name="ValidTo">Latest redeemable instant; <see langword="null"/> for no upper bound.</param>
/// <param name="IsPublic">Whether buyers see the code listed at checkout rather than having to type it.</param>
/// <param name="MaxRedemptions">Total redemption cap; <see langword="null"/> for unlimited.</param>
/// <param name="MaxRedemptionsPerBuyer">Per-buyer redemption cap; <see langword="null"/> for unlimited.</param>
/// <param name="TicketTypeIds">Ticket types to restrict the code to. Empty applies it to every type.</param>
public sealed record CreatePromoCodeCommand(
    Guid EventId,
    Guid TenantId,
    string Code,
    string? Description,
    DiscountType DiscountType,
    decimal DiscountValue,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo,
    bool IsPublic,
    int? MaxRedemptions,
    int? MaxRedemptionsPerBuyer,
    IReadOnlyList<Guid> TicketTypeIds) : IRequest<CreatePromoCodeResult>;
