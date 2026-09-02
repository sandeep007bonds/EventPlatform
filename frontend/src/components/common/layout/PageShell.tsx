import type { ReactNode } from 'react';
import { PageContainer } from './PageContainer';
import { ScrollRegion } from './ScrollRegion';

/**
 * A route's frame inside the height-constrained app shell.
 *
 * `pinned` stays put while `children` scrolls beneath it — for a page whose header, summary and tab
 * row are navigation rather than content, and are worth the vertical space they cost. Omit it and
 * everything scrolls together, which is what every list page wants and what the console did before.
 *
 * Both regions are width-constrained by the same `PageContainer`, so the pinned block and the
 * scrolling content line up rather than drifting apart on a wide screen.
 */
export function PageShell({
  pinned,
  children,
  maxWidth = 1360,
}: {
  /** Content that stays visible while the body scrolls. */
  pinned?: ReactNode;
  children: ReactNode;
  maxWidth?: number | string;
}) {
  return (
    <>
      {pinned && (
        <div style={{ padding: '28px 32px 0' }}>
          <PageContainer maxWidth={maxWidth}>{pinned}</PageContainer>
        </div>
      )}
      <ScrollRegion padding={pinned ? '16px 32px 28px' : '28px 32px'}>
        <PageContainer maxWidth={maxWidth}>{children}</PageContainer>
      </ScrollRegion>
    </>
  );
}
