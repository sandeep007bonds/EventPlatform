import { useEffect, useState } from 'react';
import { Button, Card, Input, Space, Typography } from 'antd';
import { getSeatMap, type SeatMapResponse } from '../../../services/catalog/catalogApi';
import {
  blockSeats,
  getInventorySeats,
  unblockSeats,
  type SeatInventoryStatus,
} from '../../../services/inventory/inventoryApi';
import { toast } from '../../../components/common/feedback/toast';
import { SeatGrid } from '../../../components/common/seatmap/SeatGrid';
import { SeatChip } from '../../../components/common/seatmap/SeatChip';

const STATUS_COLOR: Record<SeatInventoryStatus, string> = {
  Available: '#eef1f3',
  Held: '#ffe1a8',
  Sold: '#e2e2e2',
  Blocked: '#ffccc7',
};

const LEGEND: { label: string; color: string }[] = [
  { label: 'Available', color: '#eef1f3' },
  { label: 'Blocked', color: '#ffccc7' },
  { label: 'Held / Sold (locked)', color: '#e2e2e2' },
];

/** Organizer seat block/unblock — reuses the same per-seat-status endpoint the buyer picker uses. */
export function SeatBlockPanel({ eventId }: { eventId: string }) {
  const [seatMap, setSeatMap] = useState<SeatMapResponse | null>(null);
  const [statuses, setStatuses] = useState<Map<string, SeatInventoryStatus>>(new Map());
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [reason, setReason] = useState('');
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  const load = () => {
    Promise.all([getSeatMap(eventId), getInventorySeats(eventId)])
      .then(([map, seats]) => {
        setSeatMap(map);
        setStatuses(new Map(seats.map((s) => [s.seatId, s.status])));
      })
      .catch(() => toast.error('Could not load seat inventory.'))
      .finally(() => setLoading(false));
  };

  useEffect(load, [eventId]);

  const selectedStatuses = new Set([...selected].map((seatId) => statuses.get(seatId)));
  const canBlock =
    selected.size > 0 && selectedStatuses.size === 1 && selectedStatuses.has('Available');
  const canUnblock =
    selected.size > 0 && selectedStatuses.size === 1 && selectedStatuses.has('Blocked');

  const toggleSeat = (seatId: string) => {
    const status = statuses.get(seatId);
    if (status !== 'Available' && status !== 'Blocked') {
      return;
    }

    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(seatId)) {
        next.delete(seatId);
      } else {
        next.add(seatId);
      }
      return next;
    });
  };

  const handleBlock = async () => {
    setSubmitting(true);
    try {
      await blockSeats(eventId, { seatIds: [...selected], reason: reason || undefined });
      toast.success('Seats blocked.');
      setSelected(new Set());
      setReason('');
      load();
    } catch {
      toast.error('Could not block those seats.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleUnblock = async () => {
    setSubmitting(true);
    try {
      await unblockSeats(eventId, { seatIds: [...selected] });
      toast.success('Seats unblocked.');
      setSelected(new Set());
      load();
    } catch {
      toast.error('Could not unblock those seats.');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading || !seatMap) {
    return null;
  }

  return (
    <>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'flex-start',
          flexWrap: 'wrap',
          gap: 16,
          marginBottom: 16,
        }}
      >
        <div>
          <Typography.Title level={4} style={{ margin: 0 }}>
            Seat inventory
          </Typography.Title>
          <Typography.Text type="secondary" style={{ display: 'block', marginTop: 4 }}>
            Block seats to hold them back from sale (e.g. a kill or a restricted view).
          </Typography.Text>
        </div>
        <Space size={16} wrap>
          {LEGEND.map((item) => (
            <Space key={item.label} size={6}>
              <span
                aria-hidden
                style={{
                  width: 12,
                  height: 12,
                  borderRadius: 4,
                  background: item.color,
                  display: 'inline-block',
                  border: '1px solid rgba(0,0,0,0.08)',
                }}
              />
              <Typography.Text type="secondary" style={{ fontSize: 13 }}>
                {item.label}
              </Typography.Text>
            </Space>
          ))}
        </Space>
      </div>

      <SeatGrid
        seats={seatMap.seats}
        renderSeat={(seat) => {
          const status = statuses.get(seat.id) ?? 'Sold';
          return (
            <SeatChip
              key={seat.id}
              label={String(seat.number)}
              tooltip={`${seat.label} · ${status}`}
              selected={selected.has(seat.id)}
              disabled={status !== 'Available' && status !== 'Blocked'}
              color={STATUS_COLOR[status]}
              onClick={() => toggleSeat(seat.id)}
            />
          );
        }}
      />

      <Card styles={{ body: { padding: '16px 20px' } }}>
        <Space align="center" wrap style={{ width: '100%', justifyContent: 'space-between' }}>
          <Typography.Text type="secondary">{selected.size} seat(s) selected</Typography.Text>
          <Space>
            <Input
              placeholder="Reason (optional)"
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              disabled={!canBlock}
              style={{ width: 220 }}
            />
            <Button disabled={!canBlock} loading={submitting} onClick={() => void handleBlock()}>
              Block
            </Button>
            <Button
              disabled={!canUnblock}
              loading={submitting}
              onClick={() => void handleUnblock()}
            >
              Unblock
            </Button>
          </Space>
        </Space>
      </Card>
    </>
  );
}
