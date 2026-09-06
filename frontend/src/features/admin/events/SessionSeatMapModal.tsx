import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Alert, Button, Empty, Modal, Select, Space, Spin, Table, Tag, Typography } from 'antd';
import type { AxiosError } from 'axios';
import {
  attachSessionSeatMap,
  listTicketTypes,
  setSessionAllocations,
  type EventSessionResponse,
  type TicketTypeResponse,
} from '../../../services/catalog/catalogApi';
import {
  getSeatMap,
  listSeatMaps,
  listVenues,
  type SeatMapSummaryResponse,
  type VenueSummaryResponse,
} from '../../../services/venue/venueApi';
import { blocksOf } from '../../../utils/seatMap';
import { toast } from '../../../components/common/feedback/toast';
import { formatMoney } from '../../../utils/money';

interface Block {
  code: string;
  name: string;
  kind: 'Reserved' | 'GeneralAdmission';
  capacity: number;
  /** What the venue says this block is usually sold as. A name, never a price — ADR-0041. */
  tierLabel: string | null;
}

/**
 * Attaches a Venue seat map to one performance, and maps every block of it to a ticket type.
 *
 * **This is where price meets place.** A Venue seat carries no price — that was the whole point of
 * separating the two services (ADR-0038) — so something has to say "Lower Tier is Gold", and it has
 * to say it per performance: Friday's Lower Tier can be Gold while Saturday's matinee sells the
 * same seats as Premium. The mapping is by the block's **code**, which Venue keeps stable across
 * renames precisely so this survives one.
 *
 * Every block must be mapped. A block left out is not spare capacity — it is capacity Inventory
 * never hears about, so the map would render with a hole nobody can tell from a sold-out section.
 * Catalog refuses the publish; this refuses the save, so the organizer finds out here.
 */
export function SessionSeatMapModal({
  eventId,
  currency,
  session,
  siblings,
  onClose,
  onChanged,
}: {
  eventId: string;
  /** The event's currency — ticket-type prices are shown so a block can be priced by eye. */
  currency: string;
  session: EventSessionResponse;
  /**
   * Every performance of this event, this one included. Used only to copy an existing mapping: a
   * three-night run is normally priced the same way each night, and re-entering it per night is
   * the repetition that made block codes feel like busywork.
   */
  siblings: EventSessionResponse[];
  onClose: () => void;
  onChanged: () => void;
}) {
  const [venues, setVenues] = useState<VenueSummaryResponse[]>([]);
  const [venueId, setVenueId] = useState<string | undefined>(session.venueId ?? undefined);
  const [seatMaps, setSeatMaps] = useState<{
    venueId: string;
    maps: SeatMapSummaryResponse[];
  } | null>(null);
  const [seatMapId, setSeatMapId] = useState<string | undefined>(session.seatMapId ?? undefined);
  const [blocks, setBlocks] = useState<{ seatMapId: string; blocks: Block[] } | null>(null);
  const [ticketTypes, setTicketTypes] = useState<TicketTypeResponse[]>([]);
  const [allocations, setAllocations] = useState<Map<string, string>>(
    new Map(session.allocations.map((allocation) => [allocation.code, allocation.ticketTypeId])),
  );
  const [saving, setSaving] = useState(false);

  const locked = session.status !== 'Draft';

  useEffect(() => {
    void Promise.all([listVenues().catch(() => []), listTicketTypes(eventId).catch(() => [])]).then(
      ([venueResult, typeResult]) => {
        // Archived venues stay selectable only if this performance already points at one — an
        // organizer needs to see where a published run is happening, not have it vanish.
        setVenues(
          venueResult.filter((venue) => venue.status === 'Active' || venue.id === session.venueId),
        );
        setTicketTypes(typeResult.filter((type) => type.isActive));
      },
    );
  }, [eventId, session.venueId]);

  useEffect(() => {
    if (!venueId) {
      return;
    }
    let cancelled = false;
    listSeatMaps(venueId)
      .then((maps) => {
        if (!cancelled) {
          setSeatMaps({ venueId, maps });
        }
      })
      .catch(() => toast.error('Could not load this venue’s seat maps.'));
    return () => {
      cancelled = true;
    };
  }, [venueId]);

  useEffect(() => {
    if (!seatMapId) {
      return;
    }
    let cancelled = false;
    // No version number: the published one is the only version a performance may sell against, and
    // it is what Catalog will pin on attach.
    getSeatMap(seatMapId)
      .then((map) => {
        if (!cancelled) {
          setBlocks({ seatMapId, blocks: blocksOf(map.version) });
        }
      })
      .catch(() => {
        if (!cancelled) {
          setBlocks({ seatMapId, blocks: [] });
        }
      });
    return () => {
      cancelled = true;
    };
  }, [seatMapId]);

  // Both are tagged with what they were fetched for, so a stale list is never offered against a
  // different venue or map — and no effect has to clear them synchronously.
  const availableMaps = seatMaps != null && seatMaps.venueId === venueId ? seatMaps.maps : [];
  const loadedBlocks = blocks != null && blocks.seatMapId === seatMapId ? blocks.blocks : null;
  const currentBlocks = useMemo(() => loadedBlocks ?? [], [loadedBlocks]);
  // Derived rather than a flag: "a map is picked but its blocks are not here yet" is exactly what
  // loading means, and computing it removes a state that could disagree with reality.
  const loadingBlocks = seatMapId != null && loadedBlocks == null;
  const unallocated = currentBlocks.filter((block) => !allocations.get(block.code));

  // Two sources, and neither decides anything — a suggestion is filled in, shown, and editable
  // before it is saved. The server still validates every block against a real active ticket type.
  //
  // A sibling already mapped against this exact map wins: it is this organizer's own answer for
  // this run, so it beats the venue's general habit. Only then the map's own tier labels, matched
  // to a ticket type by name (ADR-0041 — the label is a name, never a price).
  const suggestedAllocations = useMemo(() => {
    const suggestion = new Map<string, string>();
    if (currentBlocks.length === 0) {
      return suggestion;
    }

    const twin = siblings.find(
      (other) =>
        other.id !== session.id && other.seatMapId === seatMapId && other.allocations.length > 0,
    );
    if (twin) {
      for (const allocation of twin.allocations) {
        suggestion.set(allocation.code, allocation.ticketTypeId);
      }
    }

    const typeIdsByName = new Map(
      ticketTypes.map((type) => [type.name.trim().toLowerCase(), type.id]),
    );
    for (const block of currentBlocks) {
      if (suggestion.has(block.code) || !block.tierLabel) {
        continue;
      }
      const match = typeIdsByName.get(block.tierLabel.trim().toLowerCase());
      if (match) {
        suggestion.set(block.code, match);
      }
    }

    // Only blocks this map actually has — a twin mapped against an older shape may name others.
    const codes = new Set(currentBlocks.map((block) => block.code));
    return new Map([...suggestion].filter(([code]) => codes.has(code)));
  }, [currentBlocks, siblings, session.id, seatMapId, ticketTypes]);

  const suggestedCount = [...suggestedAllocations.keys()].filter(
    (code) => !allocations.get(code),
  ).length;

  const venueTiers = [
    ...new Set(currentBlocks.map((block) => block.tierLabel).filter((label) => !!label)),
  ];

  const applySuggestions = () =>
    setAllocations((current) => {
      const next = new Map(current);
      for (const [code, ticketTypeId] of suggestedAllocations) {
        if (!next.get(code)) {
          next.set(code, ticketTypeId);
        }
      }
      return next;
    });

  const handleSave = async () => {
    if (!seatMapId) {
      return;
    }

    setSaving(true);
    try {
      // Attach first, and only when it changed: re-pinning clears the allocations server-side
      // (the block codes may not survive a version change), so doing it unconditionally would
      // wipe the very map we are about to send.
      if (seatMapId !== session.seatMapId) {
        await attachSessionSeatMap(eventId, session.id, { seatMapId });
      }

      await setSessionAllocations(
        eventId,
        session.id,
        currentBlocks.map((block) => ({
          code: block.code,
          ticketTypeId: allocations.get(block.code) as string,
        })),
      );

      toast.success('Seat map and allocations saved.');
      onChanged();
    } catch (error) {
      const body = (error as AxiosError<{ message?: string }>).response?.data;
      toast.error(body?.message ?? 'Could not save the seat map for this performance.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      open
      width={760}
      title="Seat map and pricing for this performance"
      onCancel={onClose}
      footer={
        locked
          ? [
              <Button key="close" onClick={onClose}>
                Close
              </Button>,
            ]
          : [
              <Button key="cancel" onClick={onClose}>
                Cancel
              </Button>,
              <Button
                key="save"
                type="primary"
                loading={saving}
                disabled={!seatMapId || currentBlocks.length === 0 || unallocated.length > 0}
                onClick={() => void handleSave()}
              >
                Save
              </Button>,
            ]
      }
    >
      {locked && (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
          message="Fixed now that this performance is published"
          description={
            'The seat map and its pricing are what ticket holders bought. Republishing a new ' +
            'version in the venue library does not move a performance that is already selling — ' +
            'it stays pinned to the version it sold against.'
          }
        />
      )}

      <Space direction="vertical" size={12} style={{ width: '100%', marginBottom: 20 }}>
        <div>
          <Typography.Text strong>Venue</Typography.Text>
          <Select
            value={venueId}
            disabled={locked}
            onChange={(value) => {
              setVenueId(value);
              setSeatMapId(undefined);
              setAllocations(new Map());
            }}
            placeholder="Select a venue"
            style={{ width: '100%', marginTop: 4 }}
            options={venues.map((venue) => ({
              value: venue.id,
              label: `${venue.name} — ${venue.city}, ${venue.country}`,
            }))}
            notFoundContent={
              <span>
                No active venues yet — <Link to="/admin/venues">create one under Venues</Link>, then
                come back.
              </span>
            }
          />
        </div>

        <div>
          <Typography.Text strong>Seat map</Typography.Text>
          <Select
            value={seatMapId}
            disabled={locked || !venueId}
            onChange={(value) => {
              setSeatMapId(value);
              // A different map has different block codes, so nothing carries over.
              setAllocations(value === session.seatMapId ? allocationsOf(session) : new Map());
            }}
            placeholder="Select a published seat map"
            style={{ width: '100%', marginTop: 4 }}
            options={availableMaps.map((map) => ({
              value: map.id,
              // Only a published version can be sold against, so an unpublished map is offered
              // but disabled rather than hidden — "where is my map" is a worse question than
              // "why is it greyed out".
              disabled: map.publishedVersionNumber == null,
              label:
                map.publishedVersionNumber == null
                  ? `${map.name} — no published version yet`
                  : `${map.name} — v${map.publishedVersionNumber}`,
            }))}
            notFoundContent={
              venueId ? (
                <span>
                  No seat maps here yet —{' '}
                  <Link to={`/admin/venues/${venueId}`}>add one on the venue</Link> and publish it.
                </span>
              ) : (
                'Pick a venue first.'
              )
            }
          />
        </div>
      </Space>

      {loadingBlocks ? (
        <Spin />
      ) : ticketTypes.length === 0 ? (
        // Allocation binds a block to a ticket type, so with none defined every row's dropdown
        // would be empty and the reason invisible. Ticket types genuinely come first.
        <Alert
          type="info"
          showIcon
          message="This event has no ticket types yet"
          description={
            <span>
              A block is sold <em>as</em> something, so define the ticket types first on the{' '}
              <Link to={`/admin/events/${eventId}?tab=tickets`}>Tickets &amp; pricing</Link> tab — a
              venue&rsquo;s seat map carries no prices of its own.
              {venueTiers.length > 0 && (
                <>
                  {' '}
                  This venue usually sells these blocks as <strong>
                    {venueTiers.join(', ')}
                  </strong>{' '}
                  — name them that and every block maps itself.
                </>
              )}
            </span>
          }
        />
      ) : currentBlocks.length === 0 ? (
        <Empty description="Pick a venue and a published seat map to allocate its blocks." />
      ) : (
        <>
          {unallocated.length > 0 && !locked && (
            <Alert
              type="warning"
              showIcon
              style={{ marginBottom: 12 }}
              message={`${unallocated.length} block${unallocated.length === 1 ? '' : 's'} still needs a ticket type`}
              description="Every block must sell as something. One left unmapped is capacity Inventory never hears about."
            />
          )}
          {suggestedCount > 0 && !locked && (
            // Offered, never applied silently. The organizer sees what changed before saving, and
            // the suggestion is only ever a starting point — this night is free to price
            // differently from the last one, which is why the mapping is per performance at all.
            <Alert
              type="info"
              showIcon
              style={{ marginBottom: 12 }}
              message={`${suggestedCount} block${suggestedCount === 1 ? '' : 's'} can be filled in for you`}
              description="From another performance of this event mapped against the same seat map, or from the tiers the venue named on its blocks."
              action={
                <Button size="small" onClick={applySuggestions}>
                  Fill them in
                </Button>
              }
            />
          )}
          <Table<Block>
            rowKey="code"
            dataSource={currentBlocks}
            pagination={false}
            size="small"
            columns={[
              {
                title: 'Block',
                key: 'block',
                render: (_, block) => (
                  <Space direction="vertical" size={0}>
                    <Typography.Text strong>{block.name}</Typography.Text>
                    <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                      {block.code}
                      {block.tierLabel ? ` · usually ${block.tierLabel}` : ''}
                    </Typography.Text>
                  </Space>
                ),
              },
              {
                title: 'Kind',
                key: 'kind',
                render: (_, block) => (
                  <Tag>{block.kind === 'Reserved' ? 'Reserved seats' : 'General admission'}</Tag>
                ),
              },
              { title: 'Capacity', dataIndex: 'capacity', key: 'capacity' },
              {
                title: 'Sells as',
                key: 'ticketType',
                render: (_, block) => (
                  <Select
                    value={allocations.get(block.code)}
                    disabled={locked}
                    placeholder="Pick a ticket type"
                    style={{ minWidth: 220 }}
                    status={allocations.get(block.code) ? undefined : 'warning'}
                    onChange={(ticketTypeId: string) =>
                      setAllocations((previous) => {
                        const next = new Map(previous);
                        next.set(block.code, ticketTypeId);
                        return next;
                      })
                    }
                    options={ticketTypes.map((type) => ({
                      value: type.id,
                      label: `${type.name} — ${formatMoney(type.priceMinor, currency)}`,
                    }))}
                    notFoundContent="No active ticket types — add one on the Tickets & pricing tab."
                  />
                ),
              },
            ]}
          />
        </>
      )}
    </Modal>
  );
}

/** The performance's saved allocations, as the code → ticket-type map this modal edits. */
function allocationsOf(session: EventSessionResponse): Map<string, string> {
  return new Map(
    session.allocations.map((allocation) => [allocation.code, allocation.ticketTypeId]),
  );
}
