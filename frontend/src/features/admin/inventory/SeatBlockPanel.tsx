import { useEffect, useState } from 'react';
import { Alert, Button, Card, Col, Input, Row, Space, Statistic, Typography } from 'antd';
import { getSeatMap, type SeatMapResponse } from '../../../services/catalog/catalogApi';
import {
  blockSeats,
  getGeneralAdmissionAllocations,
  getInventorySeats,
  unblockSeats,
  type GeneralAdmissionAllocationResponse,
  type SeatInventoryStatus,
} from '../../../services/inventory/inventoryApi';
import { getEventTickets, type TicketResponse } from '../../../services/ticketing/ticketingApi';
import { toast } from '../../../components/common/feedback/toast';
import { LoadError } from '../../../components/common/errors/LoadError';
import { SeatGrid } from '../../../components/common/seatmap/SeatGrid';
import { SeatChip } from '../../../components/common/seatmap/SeatChip';

// Inventory is provisioned asynchronously (pub/sub off Catalog's EventPublished, via the outbox
// relay), so the per-seat status endpoint can briefly return an empty list right after a publish
// — poll rather than trust a single fetch, mirroring OrderPage.tsx's ticket-polling pattern.
const INVENTORY_POLL_INTERVAL_MS = 1500;
const INVENTORY_POLL_MAX_ATTEMPTS = 6;

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

/** Organizer seat block/unblock — reuses the same per-seat-status endpoint the buyer picker uses. */
export function SeatBlockPanel({ eventId }: { eventId: string }) {
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
      getInventorySeats(eventId)
        .then((seats) => {
          if (cancelled) {
            return;
          }
          if (seats.length > 0 || map.seats.length === 0) {
            setStatuses(new Map(seats.map((s) => [s.seatId, s.status])));
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

    getSeatMap(eventId)
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

    getEventTickets(eventId)
      .then((tickets: TicketResponse[]) => {
        if (cancelled) {
          return;
        }
        setCheckedInSeatIds(
          new Set(
            tickets
              .filter((ticket) => ticket.status === 'CheckedIn' && ticket.seatId !== null)
              .map((ticket) => ticket.seatId as string),
          ),
        );

        const gaCounts = new Map<string, number>();
        for (const ticket of tickets) {
          if (ticket.status === 'CheckedIn' && ticket.generalAdmissionAllocationId !== null) {
            const allocationId = ticket.generalAdmissionAllocationId;
            gaCounts.set(allocationId, (gaCounts.get(allocationId) ?? 0) + 1);
          }
        }
        setCheckedInGaCounts(gaCounts);
      })
      .catch(() => {
        // Non-critical — the seat grid still works with plain Inventory statuses if this fails.
      });

    getGeneralAdmissionAllocations(eventId)
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
  }, [eventId, reloadToken]);

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
      setReloadToken((token) => token + 1);
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
        description="Could not load seat inventory."
        onRetry={() => {
          setLoading(true);
          setReloadToken((token) => token + 1);
        }}
      />
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
          seats={seatMap.seats}
          renderSeat={(seat) => {
            const status = statuses.get(seat.id) ?? 'Sold';
            const checkedIn = checkedInSeatIds.has(seat.id);
            return (
              <SeatChip
                key={seat.id}
                label={String(seat.number)}
                tooltip={checkedIn ? `${seat.label} · Checked in` : `${seat.label} · ${status}`}
                selected={selected.has(seat.id)}
                disabled={status !== 'Available' && status !== 'Blocked'}
                color={checkedIn ? CHECKED_IN_COLOR : STATUS_COLOR[status]}
                onClick={() => toggleSeat(seat.id)}
              />
            );
          }}
        />
      )}

      {seatMap.generalAdmissionSections.length > 0 && (
        <>
          <Typography.Title level={5} style={{ marginTop: 24 }}>
            General admission
          </Typography.Title>
          <Row gutter={[16, 16]} style={{ marginBottom: 16 }}>
            {seatMap.generalAdmissionSections.map((section) => {
              const allocation = gaAllocations.find((a) => a.catalogSectionId === section.id);
              const checkedIn = allocation
                ? (checkedInGaCounts.get(allocation.allocationId) ?? 0)
                : 0;
              return (
                <Col key={section.id} xs={24} sm={12} md={8}>
                  <Card size="small" title={`${section.sectionName} · ${section.priceTier}`}>
                    <Row gutter={16}>
                      <Col span={12}>
                        <Statistic
                          title="Capacity"
                          value={allocation?.totalCapacity ?? section.capacity}
                        />
                      </Col>
                      <Col span={12}>
                        <Statistic
                          title="Remaining"
                          value={allocation?.remaining ?? section.capacity}
                        />
                      </Col>
                      <Col span={12}>
                        <Statistic title="Sold" value={allocation?.soldCount ?? 0} />
                      </Col>
                      <Col span={12}>
                        <Statistic title="Held" value={allocation?.heldCount ?? 0} />
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
