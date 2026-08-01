import { useEffect, useState } from 'react';
import { Button, Card, InputNumber, Space, Typography } from 'antd';
import type { AxiosError } from 'axios';
import { useNavigate, useParams } from 'react-router-dom';
import { getEvent, getSeatMap, type SeatMapResponse } from '../../../services/catalog/catalogApi';
import {
  getInventorySeats,
  placeHold,
  type SeatInventoryStatus,
} from '../../../services/inventory/inventoryApi';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { PageHeader } from '../../../components/common/layout/PageHeader';
import { SeatGrid } from '../../../components/common/seatmap/SeatGrid';
import { SeatChip } from '../../../components/common/seatmap/SeatChip';
import { toast } from '../../../components/common/feedback/toast';
import { formatMoney } from '../../../utils/money';

const MAX_SEATS = 10;
const MAX_GENERAL_ADMISSION_QUANTITY = 10;

const STATUS_COLOR: Record<SeatInventoryStatus, string> = {
  Available: '#eef1f3',
  Held: '#ffe1a8',
  Sold: '#e2e2e2',
  Blocked: '#e2e2e2',
};

const LEGEND: { label: string; color: string }[] = [
  { label: 'Available', color: '#eef1f3' },
  { label: 'Held', color: '#ffe1a8' },
  { label: 'Sold / Blocked', color: '#e2e2e2' },
];

interface ConflictBody {
  message?: string;
  seatId?: string;
  allocationId?: string;
}

/**
 * Interactive picker: reserved sections render as a real seat grid (rows of seats, per-seat
 * availability, click to select); general-admission sections render as a quantity stepper per
 * tier. Both can be held together in one request, summarized in a sticky bottom bar.
 */
export function SeatSelectionPage() {
  const { id: eventId } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [seatMap, setSeatMap] = useState<SeatMapResponse | null>(null);
  const [currency, setCurrency] = useState('USD');
  const [statuses, setStatuses] = useState<Map<string, SeatInventoryStatus>>(new Map());
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [gaQuantities, setGaQuantities] = useState<Map<string, number>>(new Map());
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  const refreshStatuses = async (evId: string) => {
    const seats = await getInventorySeats(evId);
    setStatuses(new Map(seats.map((s) => [s.seatId, s.status])));
  };

  useEffect(() => {
    if (!eventId) {
      return;
    }

    let cancelled = false;

    Promise.all([getEvent(eventId), getSeatMap(eventId), getInventorySeats(eventId)])
      .then(([event, map, seats]) => {
        if (cancelled) {
          return;
        }
        setCurrency(event.currency);
        setSeatMap(map);
        setStatuses(new Map(seats.map((s) => [s.seatId, s.status])));
      })
      .catch(() => toast.error('Could not load the seat map.'))
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [eventId]);

  const toggleSeat = (seatId: string) => {
    if (statuses.get(seatId) !== 'Available') {
      return;
    }

    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(seatId)) {
        next.delete(seatId);
        return next;
      }
      if (next.size >= MAX_SEATS) {
        toast.error(`You can select up to ${MAX_SEATS} seats.`);
        return prev;
      }
      next.add(seatId);
      return next;
    });
  };

  const setGaQuantity = (allocationId: string, quantity: number) => {
    setGaQuantities((prev) => {
      if (quantity <= 0) {
        const next = new Map(prev);
        next.delete(allocationId);
        return next;
      }

      // The server caps the SUM of general-admission quantities across all sections in one hold
      // (HoldOptions.MaxGeneralAdmissionQuantityPerHold) — each section's own stepper only clamps
      // that section individually, so the total must be checked here too.
      const otherSectionsTotal = [...prev].reduce(
        (sum, [id, existingQuantity]) => (id === allocationId ? sum : sum + existingQuantity),
        0,
      );
      if (otherSectionsTotal + quantity > MAX_GENERAL_ADMISSION_QUANTITY) {
        toast.error(
          `You can select up to ${MAX_GENERAL_ADMISSION_QUANTITY} general-admission admissions in total.`,
        );
        return prev;
      }

      const next = new Map(prev);
      next.set(allocationId, quantity);
      return next;
    });
  };

  const handleHold = async () => {
    if (!eventId || (selected.size === 0 && gaQuantities.size === 0)) {
      return;
    }

    setSubmitting(true);
    try {
      const result = await placeHold({
        eventId,
        seatIds: [...selected],
        generalAdmissionSelections: [...gaQuantities].map(([allocationId, quantity]) => ({
          allocationId,
          quantity,
        })),
      });
      void navigate(`/checkout/${result.holdId}`);
    } catch (error) {
      const body = (error as AxiosError<ConflictBody>).response?.data;
      toast.error(
        body?.seatId
          ? `Seat ${body.seatId} is no longer available — please pick again.`
          : body?.allocationId
            ? 'One of your general-admission selections is no longer available — please try again.'
            : (body?.message ?? 'Those selections are no longer available — please pick again.'),
      );
      await refreshStatuses(eventId);
      setSelected(new Set());
      setGaQuantities(new Map());
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return <DetailSkeleton />;
  }

  if (!seatMap) {
    return (
      <Typography.Text type="secondary">No seat map is available for this event.</Typography.Text>
    );
  }

  const selectedSeatsTotal = seatMap.seats
    .filter((seat) => selected.has(seat.id))
    .reduce((sum, seat) => sum + seat.priceAmount, 0);
  const gaTotal = seatMap.generalAdmissionSections.reduce(
    (sum, section) => sum + (gaQuantities.get(section.id) ?? 0) * section.priceAmount,
    0,
  );
  const selectedTotal = selectedSeatsTotal + gaTotal;
  const gaCount = [...gaQuantities.values()].reduce((a, b) => a + b, 0);
  const hasSelection = selected.size > 0 || gaQuantities.size > 0;

  return (
    <div style={{ paddingBottom: 96 }}>
      <PageHeader
        title={seatMap.name}
        description="Pick your seats and/or general-admission quantity, then hold them to check out."
        extra={
          <Space size={16}>
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
        }
      />

      {seatMap.seats.length > 0 && (
        <SeatGrid
          seats={seatMap.seats}
          renderSeat={(seat) => {
            const status = statuses.get(seat.id) ?? 'Sold';
            return (
              <SeatChip
                key={seat.id}
                label={String(seat.number)}
                tooltip={`${seat.label} · ${seat.priceTier} · ${formatMoney(seat.priceAmount * 100, currency)}`}
                selected={selected.has(seat.id)}
                disabled={status !== 'Available'}
                color={STATUS_COLOR[status]}
                onClick={() => toggleSeat(seat.id)}
              />
            );
          }}
        />
      )}

      {seatMap.generalAdmissionSections.map((section) => (
        <Card
          key={section.id}
          style={{ marginBottom: 16 }}
          styles={{ body: { padding: '18px 20px' } }}
        >
          <div
            style={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              gap: 16,
            }}
          >
            <div>
              <Typography.Text strong>{section.sectionName}</Typography.Text>
              <Typography.Text type="secondary" style={{ display: 'block', marginTop: 2 }}>
                General admission · {section.priceTier} ·{' '}
                {formatMoney(section.priceAmount * 100, currency)} per admission
              </Typography.Text>
            </div>
            <InputNumber
              min={0}
              max={MAX_GENERAL_ADMISSION_QUANTITY}
              value={gaQuantities.get(section.id) ?? 0}
              onChange={(value) => setGaQuantity(section.id, value ?? 0)}
              style={{ width: 88 }}
            />
          </div>
        </Card>
      ))}

      <div
        style={{
          position: 'fixed',
          left: 0,
          right: 0,
          bottom: 0,
          zIndex: 20,
          background: '#fff',
          borderTop: '1px solid rgba(0,0,0,0.08)',
          boxShadow: '0 -4px 16px rgba(0,0,0,0.06)',
        }}
      >
        <div
          style={{
            maxWidth: 1180,
            margin: '0 auto',
            padding: '16px 24px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: 16,
            flexWrap: 'wrap',
          }}
        >
          <div>
            <Typography.Text strong style={{ fontSize: 16 }}>
              {formatMoney(selectedTotal * 100, currency)}
            </Typography.Text>
            <Typography.Text type="secondary" style={{ display: 'block', fontSize: 13 }}>
              {selected.size} seat{selected.size === 1 ? '' : 's'}
              {gaCount > 0 ? ` + ${gaCount} general admission` : ''} selected
            </Typography.Text>
          </div>
          <Button
            type="primary"
            size="large"
            disabled={!hasSelection}
            loading={submitting}
            onClick={() => void handleHold()}
          >
            Hold selection
          </Button>
        </div>
      </div>
    </div>
  );
}
