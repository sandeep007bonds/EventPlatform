import { useEffect, useRef, useState } from 'react';
import { subscribeRequestActivity } from '../../../services/http/requestActivity';

const BAR_COLOR = '#3ea8c4';
const HIDE_DELAY_MS = 300;

/**
 * A thin, fixed top-of-page bar that appears whenever any HTTP request is in flight
 * (driven by `services/http/requestActivity.ts`, which the axios interceptors in
 * `services/http/client.ts` update). No third-party dependency — a small hand-rolled
 * NProgress-style indeterminate animation: grows toward 80% while requests are pending,
 * completes to 100% and fades out once they've all settled. Mounted once near the root
 * (see `App.tsx`); renders nothing while idle.
 */
export function TopProgressBar() {
  const [visible, setVisible] = useState(false);
  const [width, setWidth] = useState(0);
  const [animated, setAnimated] = useState(false);
  const hideTimer = useRef<number | undefined>(undefined);

  useEffect(() => {
    return subscribeRequestActivity((activeCount) => {
      window.clearTimeout(hideTimer.current);

      if (activeCount > 0) {
        setVisible(true);
        setAnimated(false);
        // Two rAFs: the first commits the "no transition" reset, the second starts the
        // grow animation from that clean baseline — collapsing them into one frame can
        // silently skip the transition when a request starts mid-fade-out.
        requestAnimationFrame(() => {
          requestAnimationFrame(() => {
            setAnimated(true);
            setWidth(80);
          });
        });
        return;
      }

      setAnimated(true);
      setWidth(100);
      hideTimer.current = window.setTimeout(() => {
        setVisible(false);
        setWidth(0);
      }, HIDE_DELAY_MS);
    });
  }, []);

  if (!visible && width === 0) {
    return null;
  }

  return (
    <div
      aria-hidden="true"
      style={{
        position: 'fixed',
        insetInlineStart: 0,
        top: 0,
        height: 3,
        width: `${width}%`,
        backgroundColor: BAR_COLOR,
        opacity: visible ? 1 : 0,
        transition: animated
          ? 'width 4s cubic-bezier(0.1, 0.9, 0.1, 1), opacity 0.3s ease'
          : 'none',
        zIndex: 2000,
        pointerEvents: 'none',
      }}
    />
  );
}
