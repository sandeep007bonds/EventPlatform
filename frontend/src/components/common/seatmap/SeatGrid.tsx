import type { ReactNode } from 'react';
import { Card, Typography } from 'antd';
import type { FlatSeat } from '../../../utils/seatMap';
import { compareSeatNumbers } from '../../../utils/seatMap';

/**
 * Renders reserved seats grouped by section, then by row within each section, with each row's
 * seats sorted left to right by seat number — so the layout actually reads as a seat map instead
 * of a flat wall of buttons in whatever order the API returned them. `renderSeat` supplies the
 * interactive control per seat (buyer picker and organizer block panel each render it differently).
 */
export function SeatGrid({
  seats,
  renderSeat,
  sectionExtra,
}: {
  seats: FlatSeat[];
  renderSeat: (seat: FlatSeat) => ReactNode;
  sectionExtra?: (section: string, sectionSeats: FlatSeat[]) => ReactNode;
}) {
  // Grouped by section *name* for display, but ordered by the venue's own displayOrder, so the
  // sections read top-to-bottom the way the hall is laid out rather than alphabetically.
  const bySection = new Map<string, FlatSeat[]>();
  for (const seat of [...seats].sort((a, b) => a.sectionOrder - b.sectionOrder)) {
    const list = bySection.get(seat.sectionName) ?? [];
    list.push(seat);
    bySection.set(seat.sectionName, list);
  }

  return (
    <>
      {[...bySection.entries()].map(([section, sectionSeats]) => {
        const byRow = new Map<string, FlatSeat[]>();
        const rowOrder = new Map<string, number>();
        for (const seat of sectionSeats) {
          const list = byRow.get(seat.rowLabel) ?? [];
          list.push(seat);
          byRow.set(seat.rowLabel, list);
          rowOrder.set(seat.rowLabel, seat.rowOrder);
        }
        const rows = [...byRow.entries()].sort(
          (a, b) => (rowOrder.get(a[0]) ?? 0) - (rowOrder.get(b[0]) ?? 0),
        );

        return (
          <Card
            key={section}
            size="small"
            title={section}
            extra={sectionExtra?.(section, sectionSeats)}
            style={{ marginBottom: 16 }}
            styles={{ body: { padding: '18px 20px' } }}
          >
            <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
              {rows.map(([row, rowSeats]) => (
                <div key={row} style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                  <Typography.Text
                    type="secondary"
                    style={{ width: 24, flexShrink: 0, textAlign: 'right', fontSize: 12 }}
                  >
                    {row}
                  </Typography.Text>
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
                    {[...rowSeats]
                      .sort((a, b) => compareSeatNumbers(a.number, b.number))
                      .map((seat) => renderSeat(seat))}
                  </div>
                </div>
              ))}
            </div>
          </Card>
        );
      })}
    </>
  );
}
