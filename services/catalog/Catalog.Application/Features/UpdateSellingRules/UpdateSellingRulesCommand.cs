namespace Catalog.Application.Features.UpdateSellingRules;

/// <summary>
/// Command to set a draft event's commercial terms — the money and the rules that govern selling
/// the whole run.
/// </summary>
/// <remarks>
/// Much smaller than the old "update details": dates and the venue moved to the performances that
/// own them and are edited there. What is left is one decision for the whole event, which is why
/// it stays here.
/// </remarks>
/// <param name="Id">The event to update.</param>
/// <param name="TenantId">The caller's tenant id; must own the event.</param>
/// <param name="OnSaleAt">Enforced sales-window start (UTC) for the whole run, if set.</param>
/// <param name="MaxTicketsPerBuyer">Per-buyer ticket limit across the run; <see langword="null"/> means no limit.</param>
/// <param name="RequiresQueue">Whether to gate holds behind the Queue service's waiting room.</param>
/// <param name="TaxRatePercent">Sales-tax rate as a percentage; <see langword="null"/> means untaxed.</param>
/// <param name="TaxLabel">Display name for the tax on a receipt.</param>
/// <param name="BookingFeePerTicketMinor">Booking fee per ticket in minor currency units.</param>
public sealed record UpdateSellingRulesCommand(
    Guid Id,
    Guid TenantId,
    DateTimeOffset? OnSaleAt,
    int? MaxTicketsPerBuyer,
    bool RequiresQueue,
    decimal? TaxRatePercent,
    string? TaxLabel,
    long BookingFeePerTicketMinor) : IRequest<UpdateSellingRulesResult>;
