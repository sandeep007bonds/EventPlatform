import type { CSSProperties } from 'react';
import { Tooltip } from 'antd';
import { PRIMARY_COLOR } from '../../../theme/colors';

/**
 * One seat/admission control in a seat-map grid — a fixed-size square rather than a full Ant
 * `Button` (whose min-width/padding don't suit a dense grid of many small controls). Shared by the
 * buyer seat picker and the organizer block/unblock panel so both render seats identically.
 */
export function SeatChip({
  label,
  tooltip,
  selected,
  disabled,
  color,
  onClick,
}: {
  label: string;
  tooltip?: string;
  selected: boolean;
  disabled: boolean;
  /** Background color when not selected (conveys availability status). */
  color: string;
  onClick: () => void;
}) {
  const style: CSSProperties = {
    width: 34,
    height: 34,
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: 8,
    fontSize: 11,
    fontWeight: 600,
    lineHeight: 1,
    padding: 0,
    border: selected ? `1px solid ${PRIMARY_COLOR}` : '1px solid transparent',
    background: selected ? PRIMARY_COLOR : color,
    color: selected ? '#fff' : 'rgba(0,0,0,0.72)',
    cursor: disabled ? 'not-allowed' : 'pointer',
    opacity: disabled && !selected ? 0.5 : 1,
    boxShadow: selected ? '0 2px 6px rgba(0,0,0,0.2)' : 'none',
    transition: 'transform 100ms ease, box-shadow 100ms ease',
  };

  const button = (
    <button type="button" disabled={disabled} onClick={onClick} style={style}>
      {label}
    </button>
  );

  return tooltip ? <Tooltip title={tooltip}>{button}</Tooltip> : button;
}
