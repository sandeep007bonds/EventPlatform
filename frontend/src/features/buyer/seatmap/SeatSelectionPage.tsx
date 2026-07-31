import { useEffect, useMemo, useState } from 'react';
import { Button, Card, InputNumber, Space, Tag, Typography } from 'antd';
import type { AxiosError } from 'axios';
import { useNavigate, useParams } from 'react-router-dom';
import {
  getEvent,
  getSeatMap,
  type SeatMapResponse,
  type SeatResponse,
} from '../../../services/catalog/catalogApi';
import {
  getInventorySeats,
  placeHold,
  type SeatInventoryStatus,
} from '../../../services/inventory/inventoryApi';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { toast } from '../../../components/common/feedback/toast';
import { formatMoney } from '../../../utils/money';

const MAX_SEATS = 10;
const MAX_GENERAL_ADMISSION_QUANTITY = 10;

const STATUS_COLOR: Record<SeatInventoryStatus, string> = {
  Available: '#f0f0f0',
  Held: '#faad14',
  Sold: '#d9d9d9',
  Blocked: '#d9d9d9',
};

interface ConflictBody {
  message?: string;
  seatId?: string;
  allocationId?: string;
}

/**
 * Interactive picker: reserved sections render as a seat grid (per-seat availability, click to
 * select); general-admission sections render as a quantity stepper per tier. Both can be held
 * together in one request.
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

  const sections = useMemo(() => {
    if (!seatMap) {
      return [];
    }
    const bySection = new Map<string, SeatResponse[]>();
    for (const seat of seatMap.seats) {
      const list = bySection.get(seat.section) ?? [];
      list.push(seat);
      bySection.set(seat.section, list);
    }
    return [...bySection.entries()];
  }, [seatMap]);

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
      const next = new Map(prev);
      if (quantity <= 0) {
        next.delete(allocationId);
      } else {
        next.set(allocationId, quantity);
      }
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
  const hasSelection = selected.size > 0 || gaQuantities.size > 0;

  return (
    <div>
      <Typography.Title level={3}>{seatMap.name}</Typography.Title>

      {sections.length > 0 && (
        <>
          <Space style={{ marginBottom: 16 }}>
            <Tag color="default">Available</Tag>
            <Tag color="gold">Held</Tag>
            <Tag color="default" style={{ opacity: 0.5 }}>
              Sold / Blocked
            </Tag>
          </Space>

          {sections.map(([section, seats]) => (
            <Card key={section} title={section} style={{ marginBottom: 16 }}>
              <Space wrap>
                {seats.map((seat) => {
                  const status = statuses.get(seat.id) ?? 'Sold';
                  const isSelected = selected.has(seat.id);
                  return (
                    <Button
                      key={seat.id}
                      disabled={status !== 'Available'}
                      type={isSelected ? 'primary' : 'default'}
                      style={
                        isSelected
                          ? undefined
                          : { background: STATUS_COLOR[status], borderColor: STATUS_COLOR[status] }
                      }
                      onClick={() => toggleSeat(seat.id)}
                      title={`${seat.priceTier} · ${formatMoney(seat.priceAmount * 100, currency)}`}
                    >
                      {seat.label}
                    </Button>
                  );
                })}
              </Space>
            </Card>
          ))}
        </>
      )}

      {seatMap.generalAdmissionSections.map((section) => (
        <Card
          key={section.id}
          title={`${section.sectionName} · General Admission`}
          style={{ marginBottom: 16 }}
        >
          <Space align="center">
            <Typography.Text>
              {section.priceTier} · {formatMoney(section.priceAmount * 100, currency)} per admission
            </Typography.Text>
            <InputNumber
              min={0}
              max={MAX_GENERAL_ADMISSION_QUANTITY}
              value={gaQuantities.get(section.id) ?? 0}
              onChange={(value) => setGaQuantity(section.id, value ?? 0)}
            />
          </Space>
        </Card>
      ))}

      <Card>
        <Typography.Text strong>
          {selected.size} seat{selected.size === 1 ? '' : 's'}
          {gaQuantities.size > 0
            ? ` + ${[...gaQuantities.values()].reduce((a, b) => a + b, 0)} general admission`
            : ''}{' '}
          selected
        </Typography.Text>
        <Button
          type="primary"
          size="large"
          block
          style={{ marginTop: 12 }}
          disabled={!hasSelection}
          loading={submitting}
          onClick={() => void handleHold()}
        >
          Hold selection ({formatMoney(selectedTotal * 100, currency)})
        </Button>
      </Card>
    </div>
  );
}
