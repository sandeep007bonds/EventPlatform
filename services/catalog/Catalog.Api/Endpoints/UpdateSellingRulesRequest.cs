namespace Catalog.Api.Endpoints;

/// <summary>
/// Request body for setting a draft event's commercial terms. The tenant is taken from the caller's
/// token, never from this body (ADR-0011).
/// </summary>
/// <param name="RequiresQueue">Whether to gate holds behind the Queue service's waiting room.</param>
/// <param name="BookingFeePerTicketMinor">Booking fee per ticket in minor currency units; 0 means no fee.</param>
/// <param name="OnSaleAt">Enforced sales-window start (UTC) for the whole run, if set.</param>
/// <param name="MaxTicketsPerBuyer">Per-buyer ticket limit across the run; <see langword="null"/> means no limit.</param>
/// <param name="TaxRatePercent">Sales-tax rate as a percentage; <see langword="null"/> means untaxed.</param>
/// <param name="TaxLabel">Display name for the tax on a receipt (e.g. "GST 18%").</param>
public sealed record UpdateSellingRulesRequest(
    bool RequiresQueue,
    long BookingFeePerTicketMinor,
    DateTimeOffset? OnSaleAt = null,
    int? MaxTicketsPerBuyer = null,
    decimal? TaxRatePercent = null,
    string? TaxLabel = null);
