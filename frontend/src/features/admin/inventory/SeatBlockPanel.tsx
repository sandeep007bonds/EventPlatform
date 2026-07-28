import { useEffect, useState } from 'react';
import { Button, Card, Input, Space, Tag, Typography } from 'antd';
import { getSeatMap, type SeatMapResponse } from '../../../services/catalog/catalogApi';
import {
  blockSeats,
  getInventorySeats,
  unblockSeats,
  type SeatInventoryStatus,
} from '../../../services/inventory/inventoryApi';
import { toast } from '../../../components/common/feedback/toast';

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
    <Card title="Seat inventory">
      <Space wrap style={{ marginBottom: 16 }}>
        {seatMap.seats.map((seat) => {
          const status = statuses.get(seat.id) ?? 'Sold';
          const isSelected = selected.has(seat.id);
          return (
            <Tag.CheckableTag
              key={seat.id}
              checked={isSelected}
              onChange={() => toggleSeat(seat.id)}
              style={{ opacity: status === 'Held' || status === 'Sold' ? 0.5 : 1 }}
            >
              {seat.label} · {status}
            </Tag.CheckableTag>
          );
        })}
      </Space>

      <Typography.Text type="secondary">{selected.size} seat(s) selected</Typography.Text>

      <Space style={{ marginTop: 12, width: '100%' }}>
        <Input
          placeholder="Reason (optional)"
          value={reason}
          onChange={(event) => setReason(event.target.value)}
          disabled={!canBlock}
        />
        <Button disabled={!canBlock} loading={submitting} onClick={() => void handleBlock()}>
          Block
        </Button>
        <Button disabled={!canUnblock} loading={submitting} onClick={() => void handleUnblock()}>
          Unblock
        </Button>
      </Space>
    </Card>
  );
}
