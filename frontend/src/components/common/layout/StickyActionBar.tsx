import type { ReactNode } from 'react';
import { Space, theme } from 'antd';

/**
 * The actions for a form, pinned to the bottom of the scroll region it sits in.
 *
 * **Sticky, not fixed.** A fixed bar has to be told how wide the sidebar is and re-told whenever it
 * collapses; a sticky one is laid out by its container and needs to know nothing. It also stops
 * being sticky by itself once the content is short enough to fit, so a half-empty form does not get
 * a bar hovering over blank space.
 *
 * The negative horizontal margins let the bar's border and background span the full width of the
 * scroll region, edge to edge, while the buttons stay aligned with the form above them.
 */
export function StickyActionBar({
  children,
  /** Horizontal padding of the container this sits in, so the bar can bleed out to its edges. */
  bleed = 32,
}: {
  children: ReactNode;
  bleed?: number;
}) {
  const { token } = theme.useToken();

  return (
    <div
      style={{
        position: 'sticky',
        bottom: 0,
        zIndex: 2,
        margin: `24px -${bleed}px -28px`,
        padding: `12px ${bleed}px`,
        background: token.colorBgContainer,
        borderTop: `1px solid ${token.colorBorderSecondary}`,
        display: 'flex',
        justifyContent: 'flex-end',
      }}
    >
      <Space>{children}</Space>
    </div>
  );
}
