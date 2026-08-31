/** Formats a minor-unit amount (e.g. cents) as a localized currency string. */
export function formatMoney(amountMinor: number, currency: string): string {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency }).format(
    amountMinor / 100,
  );
}

/**
 * The number of minor units in one major unit.
 *
 * Assumes a 2-decimal currency, the same assumption `formatMoney` above already makes and the same
 * one the backend's `OrderPricingCalculator` makes — wrong for JPY and a handful of others, tracked
 * as T11. Named rather than inlined so the places that convert are greppable when that is fixed.
 */
export const MINOR_UNITS_PER_MAJOR = 100;

/** Converts a major-unit amount an organizer typed (e.g. 30) into minor units (3000). */
export function toMinor(amountMajor: number): number {
  return Math.round(amountMajor * MINOR_UNITS_PER_MAJOR);
}

/** Converts a minor-unit amount from the API (e.g. 3000) into major units (30) for an input field. */
export function toMajor(amountMinor: number): number {
  return amountMinor / MINOR_UNITS_PER_MAJOR;
}
