namespace Ordering.Application.Abstractions;

/// <summary>
/// The pricing facts the checkout saga needs from a Catalog event: what currency to charge in, and
/// what tax to add.
/// </summary>
/// <param name="Currency">ISO 4217 currency code the event is priced in.</param>
/// <param name="TaxRatePercent">
/// Sales-tax rate as a percentage, or <see langword="null"/> when the event is untaxed. Applied to
/// the **post-discount** subtotal.
/// </param>
/// <param name="TaxLabel">Display name for the tax on a receipt (e.g. <c>"GST 18%"</c>).</param>
public sealed record EventPricing(string Currency, decimal? TaxRatePercent, string? TaxLabel);
