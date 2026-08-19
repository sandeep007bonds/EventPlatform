namespace Ordering.Infrastructure;

/// <summary>
/// The subset of Catalog's event response the checkout saga needs. Deliberately narrow — Catalog's
/// full <c>EventResponse</c> carries far more, and mirroring all of it here would couple Ordering
/// to fields it never reads.
/// </summary>
/// <param name="Currency">ISO 4217 currency code the event is priced in.</param>
/// <param name="TaxRatePercent">Sales-tax rate as a percentage, or <see langword="null"/> when untaxed.</param>
/// <param name="TaxLabel">Display name for the tax on a receipt.</param>
internal sealed record CatalogEventPricing(string Currency, decimal? TaxRatePercent, string? TaxLabel);
