import { useEffect, useMemo, useState } from 'react';
import { Alert, Button, Card, Col, Input, Row, Select, Space, Statistic, Typography } from 'antd';
import type { EventResponse } from '../../../services/catalog/catalogApi';
import { getSeatMap, type SeatMapResponse } from '../../../services/venue/venueApi';
import {
  blockSeats,
  getGeneralAdmissionAllocations,
  getInventorySeats,
  unblockSeats,
  type GeneralAdmissionAllocationResponse,
  type InventorySeatResponse,
} from '../../../services/inventory/inventoryApi';
import { getSessionTickets, type TicketResponse } from '../../../services/ticketing/ticketingApi';
import { toast } from '../../../components/common/feedback/toast';
import { LoadError } from '../../../components/common/errors/LoadError';
import { SeatGrid } from '../../../components/common/seatmap/SeatGrid';
import { SeatChip } from '../../../components/common/seatmap/SeatChip';
import { admissionAreasOf, flattenSeatMap } from '../../../utils/seatMap';
import { inStartOrder, sessionLabel } from '../../../utils/eventSessions';

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
  Blocked: '#ffccc7',
};

const CHECKED_IN_COLOR = '#b7eb8f';

const LEGEND: { label: string; color: string }[] = [
  { label: 'Available', color: '#eef1f3' },
  { label: 'Blocked', color: '#ffccc7' },
  { label: 'Held / Sold (locked)', color: '#e2e2e2' },
  { label: 'Checked in', color: CHECKED_IN_COLOR },
];

/**
 * Organizer seat block/unblock, for **one performance at a time** — the same per-seat status
 * endpoint the buyer picker uses, so what an organizer sees is what a buyer sees.
 *
 * The performance selector is not a convenience. Availability is per night (ADR-0039): the same
 * seat can be sold on Friday and blocked on Saturday, so a panel that did not say which night it
 * was showing would be lying about half of them.
 */
export function SeatBlockPanel({ event }: { event: EventResponse }) {
  const sessions = useMemo(
    () => inStartOrder(event.sessions).filter((session) => session.status === 'Published'),
    [event],
  );

  const [eventSessionId, setEventSessionId] = useState<string | null>(sessions[0]?.id ?? null);
  const session = sessions.find((candidate) => candidate.id === eventSessionId) ?? null;

  if (sessions.length === 0) {
    return (
      <Typography.Text type="secondary" style={{ display: 'block', marginTop: 24 }}>
        No published performances yet — seat inventory appears once one is published.
      </Typography.Text>
    );
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
            Block seats to hold them back from sale (e.g. a kill or a restricted view). Availability
            is per performance — blocking a seat here leaves it on sale on every other night.
          </Typography.Text>
        </div>
        <Space size={16} wrap>
          <Select
            value={eventSessionId}
            onChange={setEventSessionId}
            style={{ minWidth: 260 }}
            options={sessions.map((candidate) => ({
              value: candidate.id,
              label: sessionLabel(candidate),
            }))}
          />
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

      {session?.seatMapId == null ? (
        <Typography.Text type="secondary">
          This performance has no seat map attached.
        </Typography.Text>
      ) : (
        // Keyed, so switching performance remounts with fresh state. That is what makes the
        // selection, the statuses and the loading flag reset — no effect has to clear them, which
        // is both simpler and the only way to avoid a synchronous setState in an effect body.
        <SessionSeatInventory
          key={session.id}
          eventSessionId={session.id}
          seatMapId={session.seatMapId}
          seatMapVersionNumber={session.seatMapVersionNumber}
        />
      )}
    </>
  );
}

/** The seat grid, GA counters and block/unblock controls for exactly one performance. */
function SessionSeatInventory({
  eventSessionId,
  seatMapId,
  seatMapVersionNumber,
}: {
  eventSessionId: string;
  seatMapId: string;
  seatMapVersionNumber: number | null;
}) {
  const [seatMap, setSeatMap] = useState<SeatMapResponse | null>(null);
  const [statuses, setStatuses] = useState<Map<string, SeatInventoryStatus>>(new Map());
  const [checkedInSeatIds, setCheckedInSeatIds] = useState<Set<string>>(new Set());
  const [gaAllocations, setGaAllocations] = useState<GeneralAdmissionAllocationResponse[]>([]);
  const [checkedInGaCounts, setCheckedInGaCounts] = useState<Map<string, number>>(new Map());
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [reason, setReason] = useState('');
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [statusLoadError, setStatusLoadError] = useState(false);
  const [provisioning, setProvisioning] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [reloadToken, setReloadToken] = useState(0);

  useEffect(() => {
    let cancelled = false;
    let attempts = 0;

    const pollStatuses = (map: SeatMapResponse) => {
      getInventorySeats(eventSessionId)
        .then((seats) => {
          if (cancelled) {
            return;
          }
          if (seats.length > 0 || flattenSeatMap(map.version).length === 0) {
            setStatuses(new Map(seats.map((seat) => [seat.seatId, seat.status])));
            setProvisioning(false);
            setStatusLoadError(false);
            return;
          }
          attempts += 1;
          if (attempts >= INVENTORY_POLL_MAX_ATTEMPTS) {
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
            setStatusLoadError(true);
            setProvisioning(false);
          }
        });
    };

    getSeatMap(seatMapId, seatMapVersionNumber ?? undefined)
      .then((map) => {
        if (cancelled) {
          return;
        }
        setSeatMap(map);
        setLoadError(false);
        setLoading(false);
        pollStatuses(map);
      })
      .catch(() => {
        if (!cancelled) {
          setLoadError(true);
          setLoading(false);
        }
      });

    getSessionTickets(eventSessionId)
      .then((tickets: TicketResponse[]) => {
        if (cancelled) {
          return;
        }
        setCheckedInSeatIds(
          new Set(
            tickets
              .filter((ticket) => ticket.status === 'CheckedIn' && ticket.seatId != null)
              .map((ticket) => ticket.seatId as string),
          ),
        );

        const gaCounts = new Map<string, number>();
        for (const ticket of tickets) {
          if (ticket.status === 'CheckedIn' && ticket.generalAdmissionAllocationId != null) {
            const allocationId = ticket.generalAdmissionAllocationId;
            gaCounts.set(allocationId, (gaCounts.get(allocationId) ?? 0) + 1);
          }
        }
        setCheckedInGaCounts(gaCounts);
      })
      .catch(() => {
        // Non-critical — the seat grid still works with plain Inventory statuses if this fails.
      });

    getGeneralAdmissionAllocations(eventSessionId)
      .then((allocations) => {
        if (!cancelled) {
          setGaAllocations(allocations);
        }
      })
      .catch(() => {
        // Non-critical — a mixed seat map still shows its Reserved half if this fails.
      });

    return () => {
      cancelled = true;
    };
  }, [eventSessionId, seatMapId, seatMapVersionNumber, reloadToken]);

  const seats = useMemo(() => (seatMap ? flattenSeatMap(seatMap.version) : []), [seatMap]);

  const gaBlocks = useMemo(() => {
    if (!seatMap) {
      return [];
    }
    const poolsByArea = new Map(gaAllocations.map((pool) => [pool.admissionAreaId, pool]));
    return admissionAreasOf(seatMap.version).map((area) => ({
      area,
      pool: poolsByArea.get(area.id) ?? null,
    }));
  }, [seatMap, gaAllocations]);

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
    if (!eventSessionId) {
      return;
    }
    setSubmitting(true);
    try {
      await blockSeats(eventSessionId, { seatIds: [...selected], reason: reason || undefined });
      toast.success('Seats blocked for this performance.');
      setSelected(new Set());
      setReason('');
      setReloadToken((token) => token + 1);
    } catch {
      toast.error('Could not block those seats.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleUnblock = async () => {
    if (!eventSessionId) {
      return;
    }
    setSubmitting(true);
    try {
      await unblockSeats(eventSessionId, { seatIds: [...selected] });
      toast.success('Seats unblocked for this performance.');
      setSelected(new Set());
      setReloadToken((token) => token + 1);
    } catch {
      toast.error('Could not unblock those seats.');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return null;
  }

  if (loadError || !seatMap) {
    return (
      <LoadError
        description="Could not load seat inventory for this performance."
        onRetry={() => {
          setLoading(true);
          setReloadToken((token) => token + 1);
        }}
      />
    );
  }

  return (
    <>
      {statusLoadError && (
        <Alert
          type="warning"
          showIcon
          message="Couldn't load seat status — the grid below may be out of date."
          action={
            <Button size="small" onClick={() => setReloadToken((token) => token + 1)}>
              Retry
            </Button>
          }
          style={{ marginBottom: 16 }}
        />
      )}
      {provisioning ? (
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
            const status = statuses.get(seat.seatId) ?? 'Sold';
            const checkedIn = checkedInSeatIds.has(seat.seatId);
            return (
              <SeatChip
                key={seat.seatId}
                label={seat.number}
                tooltip={checkedIn ? `${seat.label} · Checked in` : `${seat.label} · ${status}`}
                selected={selected.has(seat.seatId)}
                disabled={status !== 'Available' && status !== 'Blocked'}
                color={checkedIn ? CHECKED_IN_COLOR : STATUS_COLOR[status]}
                onClick={() => toggleSeat(seat.seatId)}
              />
            );
          }}
        />
      )}

      {gaBlocks.length > 0 && (
        <>
          <Typography.Title level={5} style={{ marginTop: 24 }}>
            General admission
          </Typography.Title>
          <Row gutter={[16, 16]} style={{ marginBottom: 16 }}>
            {gaBlocks.map(({ area, pool }) => {
              const checkedIn = pool ? (checkedInGaCounts.get(pool.allocationId) ?? 0) : 0;
              return (
                <Col key={area.id} xs={24} sm={12} md={8}>
                  <Card size="small" title={area.name}>
                    <Row gutter={16}>
                      <Col span={12}>
                        <Statistic title="Capacity" value={pool?.totalCapacity ?? area.capacity} />
                      </Col>
                      <Col span={12}>
                        <Statistic title="Remaining" value={pool?.remaining ?? area.capacity} />
                      </Col>
                      <Col span={12}>
                        <Statistic title="Sold" value={pool?.soldCount ?? 0} />
                      </Col>
                      <Col span={12}>
                        <Statistic title="Held" value={pool?.heldCount ?? 0} />
                      </Col>
                      <Col span={24}>
                        <Statistic title="Checked in" value={checkedIn} />
                      </Col>
                    </Row>
                  </Card>
                </Col>
              );
            })}
          </Row>
        </>
      )}

      <Card styles={{ body: { padding: '16px 20px' } }}>
        <Space align="center" wrap style={{ width: '100%', justifyContent: 'space-between' }}>
          <Typography.Text type="secondary">
            {selected.size === 0
              ? 'Click seats above to block or unblock them'
              : `${selected.size} seat(s) selected`}
          </Typography.Text>
          <Space>
            <Input
              placeholder="Reason (optional)"
              value={reason}
              onChange={(changed) => setReason(changed.target.value)}
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
