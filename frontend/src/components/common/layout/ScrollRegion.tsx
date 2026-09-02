import type { CSSProperties, ReactNode } from 'react';

/**
 * The one scrolling area of a page, inside a height-constrained shell.
 *
 * `minHeight: 0` is not decoration. A flex child defaults to `min-height: auto`, which refuses to
 * shrink below its content — so without it the region grows to fit the form, the shell overflows,
 * and the inner scrollbar never appears.
 *
 * Page padding lives here rather than on the layout's Content, so a `StickyActionBar` sticking to
 * `bottom: 0` lands flush against the viewport edge instead of floating above a padding strip.
 */
export function ScrollRegion({
  children,
  padding = '28px 32px',
  style,
}: {
  children: ReactNode;
  /** Padding for the scrolling content. */
  padding?: CSSProperties['padding'];
  style?: CSSProperties;
}) {
  return (
    <div style={{ flex: 1, minHeight: 0, overflowY: 'auto', padding, ...style }}>{children}</div>
  );
}
