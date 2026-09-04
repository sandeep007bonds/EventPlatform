import { useEffect, useMemo, useState } from 'react';
import { Alert, Button, Card, InputNumber, Modal, Space, Typography } from 'antd';
import type { AxiosError } from 'axios';
import dayjs from 'dayjs';
import { useNavigate, useParams } from 'react-router-dom';
import {
  getEvent,
  getEventBySlug,
  type EventResponse,
  type EventSessionResponse,
} from '../../../services/catalog/catalogApi';
import { getSeatMap, type SeatMapResponse } from '../../../services/venue/venueApi';
import {
  getGeneralAdmissionAllocations,
  getInventorySeats,
  placeHold,
  type GeneralAdmissionAllocationResponse,
  type InventorySeatResponse,
} from '../../../services/inventory/inventoryApi';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { PageHeader } from '../../../components/common/layout/PageHeader';
import { SeatGrid } from '../../../components/common/seatmap/SeatGrid';
import { SeatChip } from '../../../components/common/seatmap/SeatChip';
import { toast } from '../../../components/common/feedback/toast';
import { formatMoney } from '../../../utils/money';
import { getValidAdmissionToken } from '../../../utils/queueAdmission';
import { getSession } from '../../../services/http/tokenStore';
import { admissionAreasOf, flattenSeatMap } from '../../../utils/seatMap';
import { sessionLabel, venueLabel } from '../../../utils/eventSessions';
import { OtpLoginFlow } from '../auth/OtpLoginFlow';

const MAX_SEATS = 10;
const MAX_GENERAL_ADMISSION_QUANTITY = 10;

// Inventory is provisioned asynchronously (pub/sub off Catalog's EventSessionPublished, via the
// outbox relay), so the per-seat status endpoint can briefly return an empty list right after a
// publish — poll rather than trust a single fetch, mirroring OrderPage.tsx's ticket-polling pattern.
const INVENTORY_POLL_INTERVAL_MS = 1500;
const INVENTORY_POLL_MAX_ATTEMPTS = 6;

type SeatInventoryStatus = InventorySeatResponse['status'];

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
 * Interactive picker for **one performance**: reserved sections render as a real seat grid (rows of
 * seats, per-seat availability, click to select); admission areas render as a quantity stepper.
 * Both can be held together in one request, summarized in a sticky bottom bar.
 *
 * Two services feed this page, and the split matters. **Venue** says which seats exist and how the
 * hall is laid out — the same answer every night. **Inventory** says what each one costs tonight
 * and whether it is still free — a different answer for every performance. Prices are read from
 * Inventory, never re-derived from Catalog's ticket types, so the number on a seat is the number
 * the checkout will charge (ADR-0034).
 */
export function SeatSelectionPage() {
  const { eventSlug, eventSessionId } = useParams<{
    eventSlug: string;
    eventSessionId: string;
  }>();
  const navigate = useNavigate();

  const [event, setEvent] = useState<EventResponse | null>(null);
  const [session, setSession] = useState<EventSessionResponse | null>(null);
  const [seatMap, setSeatMap] = useState<SeatMapResponse | null>(null);
  const [seatInventory, setSeatInventory] = useState<Map<string, InventorySeatResponse>>(new Map());
  const [allocations, setAllocations] = useState<GeneralAdmissionAllocationResponse[]>([]);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  // Keyed by Inventory's own allocation id, which is what a hold request must reference. The Venue
  // admission area is a different id entirely — it identifies the block in the building, not the
  // pool of tickets for tonight.
  const [gaQuantities, setGaQuantities] = useState<Map<string, number>>(new Map());
  const [loading, setLoading] = useState(true);
  const [provisioning, setProvisioning] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [otpModalOpen, setOtpModalOpen] = useState(false);

  const refreshInventory = async (sessionId: string) => {
    const [seats, pools] = await Promise.all([
      getInventorySeats(sessionId),
      getGeneralAdmissionAllocations(sessionId),
    ]);
    setSeatInventory(new Map(seats.map((seat) => [seat.seatId, seat])));
    setAllocations(pools);
  };

  useEffect(() => {
    if (!eventSlug || !eventSessionId) {
      return;
    }

    let cancelled = false;
    let attempts = 0;

    const pollInventory = (map: SeatMapResponse) => {
      Promise.all([
        getInventorySeats(eventSessionId),
        getGeneralAdmissionAllocations(eventSessionId),
      ])
        .then(([seats, pools]) => {
          if (cancelled) {
            return;
          }

          const expectedSeats = flattenSeatMap(map.version).length;
          const seatsReady = seats.length > 0 || expectedSeats === 0;
          const poolsReady = pools.length > 0 || map.version.admissionAreas.length === 0;

          const apply = () => {
            setSeatInventory(new Map(seats.map((seat) => [seat.seatId, seat])));
            setAllocations(pools);
            setProvisioning(false);
          };

          if (seatsReady && poolsReady) {
            apply();
            return;
          }

          attempts += 1;
          if (attempts >= INVENTORY_POLL_MAX_ATTEMPTS) {
            // Set whatever came back even if incomplete — better than leaving stale/empty state.
            apply();
            return;
          }
          setProvisioning(true);
          setTimeout(() => {
            if (!cancelled) {
              pollInventory(map);
            }
          }, INVENTORY_POLL_INTERVAL_MS);
        })
        .catch(() => {
          if (!cancelled) {
            setProvisioning(false);
          }
        });
    };

    (isGuid(eventSlug) ? getEvent(eventSlug) : getEventBySlug(eventSlug))
      .then(async (eventResult) => {
        const sessionResult =
          eventResult.sessions.find((candidate) => candidate.id === eventSessionId) ?? null;

        if (cancelled) {
          return;
        }

        setEvent(eventResult);
        setSession(sessionResult);

        // Covers a direct URL hit bypassing EventDetailPage's own redirect — a buyer with no
        // valid (unexpired) admission token must go through the waiting room first. The token is
        // keyed on the *event*: one waiting room gates the on-sale, which covers the whole run.
        if (eventResult.requiresQueue && !getValidAdmissionToken(eventResult.id)) {
          void navigate(`/events/${eventResult.slug}/queue?eventSessionId=${eventSessionId}`, {
            replace: true,
          });
          return;
        }

        if (!sessionResult?.seatMapId) {
          return;
        }

        const map = await getSeatMap(
          sessionResult.seatMapId,
          sessionResult.seatMapVersionNumber ?? undefined,
        ).catch(() => null);

        if (cancelled || !map) {
          return;
        }
        setSeatMap(map);
        pollInventory(map);
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
  }, [eventSlug, eventSessionId, navigate]);

  const seats = useMemo(() => (seatMap ? flattenSeatMap(seatMap.version) : []), [seatMap]);

  // Venue's admission areas joined to the pools Inventory provisioned for tonight — the area
  // supplies the name and the ordering, the pool supplies the price and what is left.
  const gaBlocks = useMemo(() => {
    if (!seatMap) {
      return [];
    }
    const poolsByArea = new Map(allocations.map((pool) => [pool.admissionAreaId, pool]));
    return admissionAreasOf(seatMap.version)
      .map((area) => ({ area, pool: poolsByArea.get(area.id) }))
      .filter(
        (
          entry,
        ): entry is { area: (typeof entry)['area']; pool: GeneralAdmissionAllocationResponse } =>
          entry.pool != null,
      );
  }, [seatMap, allocations]);

  const maxTicketsPerBuyer = event?.maxTicketsPerBuyer ?? null;

  const toggleSeat = (seatId: string) => {
    if (seatInventory.get(seatId)?.status !== 'Available') {
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

      // The event's organizer-configured per-buyer limit caps seats + GA quantities combined, and
      // the server counts it across every performance of the run — so this is an optimistic check
      // that can still be refused at hold time by an earlier order on another night.
      if (maxTicketsPerBuyer != null) {
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

  const setGaQuantity = (allocationId: string, quantity: number) => {
    setGaQuantities((prev) => {
      if (quantity <= 0) {
        const next = new Map(prev);
        next.delete(allocationId);
        return next;
      }

      // The server caps the SUM of general-admission quantities across all areas in one hold
      // (HoldOptions.MaxGeneralAdmissionQuantityPerHold) — each area's own stepper only clamps
      // that area individually, so the total must be checked here too.
      const otherAreasTotal = [...prev].reduce(
        (sum, [id, existingQuantity]) => (id === allocationId ? sum : sum + existingQuantity),
        0,
      );
      if (otherAreasTotal + quantity > MAX_GENERAL_ADMISSION_QUANTITY) {
        toast.error(
          `You can select up to ${MAX_GENERAL_ADMISSION_QUANTITY} general-admission admissions in total.`,
        );
        return prev;
      }

      if (
        maxTicketsPerBuyer != null &&
        selected.size + otherAreasTotal + quantity > maxTicketsPerBuyer
      ) {
        toast.error(`You can select up to ${maxTicketsPerBuyer} tickets for this event.`);
        return prev;
      }

      const next = new Map(prev);
      next.set(allocationId, quantity);
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
    if (!event || !eventSessionId || (selected.size === 0 && gaQuantities.size === 0)) {
      return;
    }

    setSubmitting(true);
    try {
      const result = await placeHold({
        eventSessionId,
        seatIds: [...selected],
        generalAdmissionSelections: [...gaQuantities].map(([allocationId, quantity]) => ({
          allocationId,
          quantity,
        })),
        queueAdmissionToken: getValidAdmissionToken(event.id) ?? undefined,
      });
      void navigate(`/checkout/${result.holdId}`);
    } catch (error) {
      const body = (error as AxiosError<ConflictBody>).response?.data;
      // The admission token expired between being admitted and actually holding — send the
      // buyer back to rejoin the queue rather than just showing a dead-end error.
      if (body?.message?.includes('requires joining the queue')) {
        toast.error('Your place in the queue has expired — please rejoin.');
        void navigate(`/events/${event.slug}/queue?eventSessionId=${eventSessionId}`, {
          replace: true,
        });
        return;
      }
      toast.error(
        body?.seatId
          ? `Seat ${body.seatId} is no longer available — please pick again.`
          : body?.allocationId
            ? 'One of your general-admission selections is no longer available — please try again.'
            : (body?.message ?? 'Those selections are no longer available — please pick again.'),
      );
      await refreshInventory(eventSessionId);
      setSelected(new Set());
      setGaQuantities(new Map());
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return <DetailSkeleton />;
  }

  if (!event || !session) {
    return (
      <Typography.Text type="secondary">
        Couldn't find that performance. It may have been cancelled or rescheduled.
      </Typography.Text>
    );
  }

  if (!seatMap) {
    return (
      <Typography.Text type="secondary">
        Couldn't load the seat map for this performance — it may not be set up yet, or something
        went wrong. Please try again shortly.
      </Typography.Text>
    );
  }

  // A buyer can reach this route directly by URL, bypassing EventDetailPage's disabled button —
  // enforce the on-sale window here too. The server rejects the hold either way (OnSaleNotStarted).
  if (event.onSaleAt != null && dayjs(event.onSaleAt).isAfter(dayjs())) {
    return (
      <Typography.Text type="secondary">
        Tickets go on sale {dayjs(event.onSaleAt).format('MMMM D, YYYY · h:mm A')}.
      </Typography.Text>
    );
  }

  // Both of these are per performance now: one night can be paused, or past its booking cutoff,
  // while the rest of the run is still selling.
  if (session.salesPaused) {
    return (
      <Typography.Text type="secondary">
        Sales are currently paused for this performance. Please check back later.
      </Typography.Text>
    );
  }

  if (session.bookingEndsAt != null && dayjs(session.bookingEndsAt).isBefore(dayjs())) {
    return (
      <Typography.Text type="secondary">Booking has closed for this performance.</Typography.Text>
    );
  }

  const selectedSeatsTotal = [...selected].reduce(
    (sum, seatId) => sum + (seatInventory.get(seatId)?.priceMinor ?? 0),
    0,
  );
  const gaTotal = gaBlocks.reduce(
    (sum, { pool }) => sum + (gaQuantities.get(pool.allocationId) ?? 0) * pool.priceMinor,
    0,
  );
  const selectedTotal = selectedSeatsTotal + gaTotal;
  const gaCount = [...gaQuantities.values()].reduce((a, b) => a + b, 0);
  const hasSelection = selected.size > 0 || gaQuantities.size > 0;

  return (
    <div style={{ paddingBottom: 96 }}>
      <PageHeader
        title={event.title}
        description={`${sessionLabel(session)}${
          venueLabel(session) ? ` · ${venueLabel(session)}` : ''
        } — pick your seats and/or general-admission quantity, then hold them to check out.`}
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

      {seats.length > 0 &&
        (provisioning ? (
          <Alert
            type="info"
            showIcon
            message="Seats are still being set up — this can take a few seconds."
            style={{ marginBottom: 16 }}
          />
        ) : (
          <SeatGrid
            seats={seats}
            renderSeat={(seat) => {
              const inventory = seatInventory.get(seat.seatId);
              const status = inventory?.status ?? 'Sold';
              return (
                <SeatChip
                  key={seat.seatId}
                  label={seat.number}
                  tooltip={
                    inventory
                      ? `${seat.label} · ${formatMoney(inventory.priceMinor, event.currency)}`
                      : seat.label
                  }
                  selected={selected.has(seat.seatId)}
                  disabled={status !== 'Available'}
                  color={STATUS_COLOR[status]}
                  onClick={() => toggleSeat(seat.seatId)}
                />
              );
            }}
          />
        ))}

      {gaBlocks.map(({ area, pool }) => (
        <Card
          key={area.id}
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
              <Typography.Text strong>{area.name}</Typography.Text>
              <Typography.Text type="secondary" style={{ display: 'block', marginTop: 2 }}>
                General admission · {formatMoney(pool.priceMinor, event.currency)} per admission ·{' '}
                {pool.remaining} left
              </Typography.Text>
            </div>
            <InputNumber
              min={0}
              max={Math.min(MAX_GENERAL_ADMISSION_QUANTITY, pool.remaining)}
              value={gaQuantities.get(pool.allocationId) ?? 0}
              onChange={(value) => setGaQuantity(pool.allocationId, value ?? 0)}
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
              {formatMoney(selectedTotal, event.currency)}
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

/** Whether the route param is an event id rather than a slug — see `EventDetailPage`. */
function isGuid(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);
}
