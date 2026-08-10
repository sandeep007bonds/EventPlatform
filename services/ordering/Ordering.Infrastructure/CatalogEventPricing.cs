namespace Ordering.Infrastructure;

/// <summary>
/// The subset of Catalog's event response the checkout saga needs. Deliberately narrow — Catalog's
/// full <c>EventResponse</c> carries far more, and mirroring all of it here would couple Ordering
/// to fields it never reads.
/// </summary>
/// <param name="Currency">ISO 4217 currency code the event is priced in.</param>
internal sealed record CatalogEventPricing(string Currency);
