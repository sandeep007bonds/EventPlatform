import type { ReactNode } from 'react';

/**
 * A bordered strip for a page's filter/search controls — visually separates "how you're filtering
 * this list" from the list itself, instead of filter inputs floating loosely above a table.
 */
export function Toolbar({ children }: { children: ReactNode }) {
  return (
    <div
      style={{
        display: 'flex',
        flexWrap: 'wrap',
        gap: 12,
        alignItems: 'center',
        padding: '12px 16px',
        marginBottom: 16,
        background: 'rgba(0,0,0,0.02)',
        border: '1px solid rgba(0,0,0,0.06)',
        borderRadius: 10,
      }}
    >
      {children}
    </div>
  );
}
