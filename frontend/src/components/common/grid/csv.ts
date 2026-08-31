/**
 * CSV generation for grid exports.
 *
 * Split out from the grid component because the escaping below is the part that has to be right,
 * and it is easier to reason about (and reuse for a server-side export's filename handling) on its
 * own.
 */

/** One column's contribution to an exported row. */
export interface CsvColumn<T> {
  /** Column heading in the exported file. */
  header: string;
  /** The cell's value. Return a primitive — a React node is not exportable. */
  value: (row: T) => string | number | boolean | null | undefined;
}

/**
 * Renders rows as an RFC 4180 CSV string.
 *
 * Two things are deliberate here.
 *
 * **Quoting**: a field is quoted when it contains a comma, a quote or a newline, and embedded
 * quotes are doubled. Without that, one venue called `Hall A, Level 2` silently shifts every
 * following column in the row.
 *
 * **Formula injection**: a field starting `=`, `+`, `-`, `@`, tab or CR is prefixed with a single
 * quote. Spreadsheet software treats those as formulas, so an attacker-controlled field — an event
 * title, a buyer's name — can become `=HYPERLINK(...)` or a command the recipient's Excel runs on
 * open. The export is a file we hand to an organizer, so this is our problem, not theirs.
 */
export function toCsv<T>(rows: readonly T[], columns: readonly CsvColumn<T>[]): string {
  const header = columns.map((column) => escapeField(column.header)).join(',');
  const body = rows.map((row) =>
    columns.map((column) => escapeField(formatValue(column.value(row)))).join(','),
  );
  return [header, ...body].join('\r\n');
}

/** Triggers a browser download of `content` as `fileName`. */
export function downloadCsv(fileName: string, content: string): void {
  // The BOM is what makes Excel read the file as UTF-8; without it, currency symbols and
  // non-Latin names in an exported list arrive mojibaked.
  const blob = new Blob(['\uFEFF', content], { type: 'text/csv;charset=utf-8;' });
  downloadBlob(fileName, blob);
}

/** Triggers a browser download of an already-built blob — used by server-side exports. */
export function downloadBlob(fileName: string, blob: Blob): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.append(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

/** A timestamped file name, so repeated exports don't overwrite each other in Downloads. */
export function timestampedFileName(base: string): string {
  const stamp = new Date().toISOString().slice(0, 19).replaceAll(':', '-');
  return `${base}-${stamp}.csv`;
}

function formatValue(value: string | number | boolean | null | undefined): string {
  if (value === null || value === undefined) {
    return '';
  }
  return String(value);
}

const FORMULA_PREFIXES = ['=', '+', '-', '@', '\t', '\r'];

/** A plain decimal literal, optionally signed — the one thing a leading `-` is allowed to start. */
const NUMERIC = /^[+-]?\d+(\.\d+)?$/;

function escapeField(value: string): string {
  // Numbers are exempt from the formula guard. Without this a discount of -1234 exports as the
  // text '-1234, which no longer sums — and money columns are exactly what an organizer opens a
  // CSV to add up. A real formula never parses as a bare number, so nothing is let through.
  const needsGuard = !NUMERIC.test(value) && FORMULA_PREFIXES.some((p) => value.startsWith(p));
  const guarded = needsGuard ? `'${value}` : value;
  return /[",\r\n]/.test(guarded) ? `"${guarded.replaceAll('"', '""')}"` : guarded;
}
