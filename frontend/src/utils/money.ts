/** Formats a minor-unit amount (e.g. cents) as a localized currency string. */
export function formatMoney(amountMinor: number, currency: string): string {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency }).format(
    amountMinor / 100,
  );
}
