import type { CSSProperties, ReactNode } from 'react';

/**
 * Centers page content within a comfortable reading width, with responsive horizontal padding —
 * every route's content sits inside one of these rather than stretching edge-to-edge on wide
 * screens. `maxWidth` defaults to the buyer storefront's comfortable width; admin pages that need
 * more room for tables pass a wider value.
 */
export function PageContainer({
  children,
  maxWidth = 1180,
  style,
}: {
  children: ReactNode;
  maxWidth?: number | string;
  style?: CSSProperties;
}) {
  return <div style={{ maxWidth, margin: '0 auto', width: '100%', ...style }}>{children}</div>;
}
