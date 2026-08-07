import { useEffect, useState } from 'react';
import { Alert, Button, Card, InputNumber, Modal, Space, Typography } from 'antd';
import type { AxiosError } from 'axios';
import dayjs from 'dayjs';
import { useNavigate, useParams } from 'react-router-dom';
import { getEvent, getSeatMap, type SeatMapResponse } from '../../../services/catalog/catalogApi';
import {
  getGeneralAdmissionAllocations,
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
import { getValidAdmissionToken } from '../../../utils/queueAdmission';
import { getSession } from '../../../services/http/tokenStore';
import { OtpLoginFlow } from '../auth/OtpLoginFlow';

const MAX_SEATS = 10;
const MAX_GENERAL_ADMISSION_QUANTITY = 10;

// Inventory is provisioned asynchronously (pub/sub off Catalog's EventPublished, via the outbox
// relay), so the per-seat status endpoint can briefly return an empty list right after a publish
// — poll rather than trust a single fetch, mirroring OrderPage.tsx's ticket-polling pattern.
const INVENTORY_POLL_INTERVAL_MS = 1500;
const INVENTORY_POLL_MAX_ATTEMPTS = 6;

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
  const [maxTicketsPerBuyer, setMaxTicketsPerBuyer] = useState<number | null>(null);
  const [onSaleAt, setOnSaleAt] = useState<string | null>(null);
  const [salesPaused, setSalesPaused] = useState(false);
  const [statuses, setStatuses] = useState<Map<string, SeatInventoryStatus>>(new Map());
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [gaQuantities, setGaQuantities] = useState<Map<string, number>>(new Map());
  // Catalog's seat map only knows its own section id — Inventory provisions each general-admission
  // allocation with its own separately-generated id (only linked via a CatalogSectionId foreign
  // field), so a hold request must reference *this* map's values, never seatMap.generalAdmissionSections[].id.
  const [gaAllocationIdsBySection, setGaAllocationIdsBySection] = useState<Map<string, string>>(
    new Map(),
  );
  const [loading, setLoading] = useState(true);
  const [provisioning, setProvisioning] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [otpModalOpen, setOtpModalOpen] = useState(false);

  const refreshStatuses = async (evId: string) => {
    const [seats, allocations] = await Promise.all([
      getInventorySeats(evId),
      getGeneralAdmissionAllocations(evId),
    ]);
    setStatuses(new Map(seats.map((s) => [s.seatId, s.status])));
    setGaAllocationIdsBySection(
      new Map(allocations.map((a) => [a.catalogSectionId, a.allocationId])),
    );
  };

  useEffect(() => {
    if (!eventId) {
      return;
    }

    let cancelled = false;
    let attempts = 0;

    const pollStatuses = (map: SeatMapResponse) => {
      Promise.all([getInventorySeats(eventId), getGeneralAdmissionAllocations(eventId)])
        .then(([seats, allocations]) => {
          if (cancelled) {
            return;
          }

          const seatsReady = seats.length > 0 || map.seats.length === 0;
          const allocationsReady =
            allocations.length > 0 || map.generalAdmissionSections.length === 0;

          if (seatsReady && allocationsReady) {
            setStatuses(new Map(seats.map((s) => [s.seatId, s.status])));
            setGaAllocationIdsBySection(
              new Map(allocations.map((a) => [a.catalogSectionId, a.allocationId])),
            );
            setProvisioning(false);
            return;
          }

          attempts += 1;
          if (attempts >= INVENTORY_POLL_MAX_ATTEMPTS) {
            // Set whatever came back even if incomplete — better than leaving stale/empty state.
            setStatuses(new Map(seats.map((s) => [s.seatId, s.status])));
            setGaAllocationIdsBySection(
              new Map(allocations.map((a) => [a.catalogSectionId, a.allocationId])),
            );
            setProvisioning(false);
            return;
          }
          setProvisioning(true);
          setTimeout(() => {
            if (!cancelled) {
              pollStatuses(map);
            }
          }, INVENTORY_POLL_INTERVAL_MS);
        })
        .catch(() => {
          if (!cancelled) {
            setProvisioning(false);
          }
        });
    };

    Promise.all([getEvent(eventId), getSeatMap(eventId)])
      .then(([event, map]) => {
        if (cancelled) {
          return;
        }
        // Covers a direct URL hit bypassing EventDetailPage's own redirect — a buyer with no
        // valid (unexpired) admission token must go through the waiting room first.
        if (event.requiresQueue && !getValidAdmissionToken(eventId)) {
          void navigate(`/events/${eventId}/queue`, { replace: true });
          return;
        }
        setCurrency(event.currency);
        setMaxTicketsPerBuyer(event.maxTicketsPerBuyer);
        setOnSaleAt(event.onSaleAt);
        setSalesPaused(event.salesPaused);
        setSeatMap(map);
        pollStatuses(map);
      })
      .catch(() => {
        // seatMap stays null — the render below already shows a graceful message for that,
        // covering both "genuinely not defined yet" and "failed to load" with one honest state.
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [eventId, navigate]);

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

      // The event's organizer-configured per-buyer limit caps seats + GA quantities combined.
      if (maxTicketsPerBuyer !== null) {
        const gaCount = [...gaQuantities.values()].reduce((a, b) => a + b, 0);
        if (next.size + 1 + gaCount > maxTicketsPerBuyer) {
          toast.error(`You can select up to ${maxTicketsPerBuyer} tickets for this event.`);
          return prev;
        }
      }

      next.add(seatId);
      return next;
    });
  };

  // gaQuantities is keyed by Catalog's section id (what the seat-map UI naturally has on hand) —
  // never Inventory's own allocationId. Translated to the real allocation id only when building the
  // hold request, via gaAllocationIdsBySection (see handleHold).
  const setGaQuantity = (catalogSectionId: string, quantity: number) => {
    setGaQuantities((prev) => {
      if (quantity <= 0) {
        const next = new Map(prev);
        next.delete(catalogSectionId);
        return next;
      }

      // The server caps the SUM of general-admission quantities across all sections in one hold
      // (HoldOptions.MaxGeneralAdmissionQuantityPerHold) — each section's own stepper only clamps
      // that section individually, so the total must be checked here too.
      const otherSectionsTotal = [...prev].reduce(
        (sum, [id, existingQuantity]) => (id === catalogSectionId ? sum : sum + existingQuantity),
        0,
      );
      if (otherSectionsTotal + quantity > MAX_GENERAL_ADMISSION_QUANTITY) {
        toast.error(
          `You can select up to ${MAX_GENERAL_ADMISSION_QUANTITY} general-admission admissions in total.`,
        );
        return prev;
      }

      // The event's organizer-configured per-buyer limit caps seats + GA quantities combined.
      if (
        maxTicketsPerBuyer !== null &&
        selected.size + otherSectionsTotal + quantity > maxTicketsPerBuyer
      ) {
        toast.error(`You can select up to ${maxTicketsPerBuyer} tickets for this event.`);
        return prev;
      }

      const next = new Map(prev);
      next.set(catalogSectionId, quantity);
      return next;
    });
  };

  const handleHold = async () => {
    // Read the session store directly, not the `user` from useAuth(): onVerified below calls
    // handleHold() synchronously, in the same tick AuthContext's setUser(...) runs — before React
    // has re-rendered this component with the new user, so a closure over `user` would still see
    // null here and just reopen the modal. getSession() is a plain module-level read, updated the
    // instant loginWithOtp() calls setSession(), so it reflects the fresh login immediately.
    if (!getSession()) {
      // The identity gate lives here, not an upfront login wall — a buyer picks seats freely
      // and only verifies via OTP at the moment they actually claim scarce inventory (ADR-0016).
      setOtpModalOpen(true);
      return;
    }
    if (!eventId || (selected.size === 0 && gaQuantities.size === 0)) {
      return;
    }

    setSubmitting(true);
    try {
      const result = await placeHold({
        eventId,
        seatIds: [...selected],
        generalAdmissionSelections: [...gaQuantities]
          .map(([catalogSectionId, quantity]) => ({
            allocationId: gaAllocationIdsBySection.get(catalogSectionId),
            quantity,
          }))
          .filter((selection): selection is { allocationId: string; quantity: number } =>
            Boolean(selection.allocationId),
          ),
        queueAdmissionToken: getValidAdmissionToken(eventId) ?? undefined,
      });
      void navigate(`/checkout/${result.holdId}`);
    } catch (error) {
      const body = (error as AxiosError<ConflictBody>).response?.data;
      // The admission token expired between being admitted and actually holding — send the
      // buyer back to rejoin the queue rather than just showing a dead-end error.
      if (body?.message?.includes('requires joining the queue')) {
        toast.error('Your place in the queue has expired — please rejoin.');
        void navigate(`/events/${eventId}/queue`, { replace: true });
        return;
      }
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
      <Typography.Text type="secondary">
        Couldn't load the seat map for this event — it may not be defined yet, or something went
        wrong. Please try again shortly.
      </Typography.Text>
    );
  }

  // A buyer can reach this route directly by URL, bypassing EventDetailPage's disabled button —
  // enforce the on-sale window here too. The server rejects the hold either way (OnSaleNotStarted).
  if (onSaleAt !== null && dayjs(onSaleAt).isAfter(dayjs())) {
    return (
      <Typography.Text type="secondary">
        Tickets go on sale {dayjs(onSaleAt).format('MMMM D, YYYY · h:mm A')}.
      </Typography.Text>
    );
  }

  // A buyer can reach this route directly by URL, bypassing EventDetailPage's disabled button —
  // enforce the manual sales pause here too. The server rejects the hold either way (SalesPaused).
  if (salesPaused) {
    return (
      <Typography.Text type="secondary">
        Sales are currently paused for this event. Please check back later.
      </Typography.Text>
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

      {seatMap.seats.length > 0 &&
        (provisioning ? (
          <Alert
            type="info"
            showIcon
            message="Seats are still being set up — this can take a few seconds."
            style={{ marginBottom: 16 }}
          />
        ) : (
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
        ))}

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

      <Modal
        open={otpModalOpen}
        onCancel={() => setOtpModalOpen(false)}
        footer={null}
        title="Log in to hold your seats"
        maskClosable={false}
      >
        <OtpLoginFlow
          onVerified={() => {
            setOtpModalOpen(false);
            void handleHold();
          }}
        />
      </Modal>
    </div>
  );
}
