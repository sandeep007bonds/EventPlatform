import { Alert, Card, Empty, Space, Table, Tag, Typography } from 'antd';
import type { EventResponse, EventSessionResponse } from '../../../services/catalog/catalogApi';
import { formatEventDateTime } from '../../../utils/eventTime';
import { inStartOrder, venueLabel } from '../../../utils/eventSessions';

const SESSION_STATUS_COLOR: Record<EventSessionResponse['status'], string> = {
  Draft: 'default',
  Published: 'green',
  Cancelled: 'red',
  Completed: 'blue',
};

/**
 * The performances of an event — one row per night, with the venue and seat-map version each one
 * pinned and how many of its blocks have been allocated to a ticket type.
 *
 * Read-only for now. The model landed before the editor did, and showing an organizer what the
 * server actually holds is more useful than showing them nothing until the full editor exists —
 * particularly the allocation count, which is what a publish will be refused for.
 */
export function EventPerformancesPanel({ event }: { event: EventResponse }) {
  const sessions = inStartOrder(event.sessions);

  if (sessions.length === 0) {
    return (
      <Card>
        <Empty description="This event has no performances yet." />
      </Card>
    );
  }

  return (
    <div>
      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
        message="A performance is what gets sold"
        description={
          'Inventory, orders, tickets and scans all hang off a performance, not the event — so ' +
          'holding a seat on one night leaves it free on the next, and a scanner at the door ' +
          'turns away a ticket for another night. Each one names a published seat-map version ' +
          'from your venue library, and allocates every block of it to a ticket type.'
        }
      />

      <Table<EventSessionResponse>
        rowKey="id"
        dataSource={sessions}
        pagination={false}
        size="middle"
        columns={[
          {
            title: 'Performance',
            key: 'when',
            render: (_, session) => (
              <Space direction="vertical" size={0}>
                <Typography.Text strong>
                  {formatEventDateTime(session.startsAt, session.timeZoneId)}
                </Typography.Text>
                {session.name && <Typography.Text type="secondary">{session.name}</Typography.Text>}
              </Space>
            ),
          },
          {
            title: 'Doors',
            key: 'doors',
            render: (_, session) =>
              session.doorsOpenAt
                ? formatEventDateTime(session.doorsOpenAt, session.timeZoneId)
                : '—',
          },
          {
            title: 'Booking closes',
            key: 'bookingEndsAt',
            render: (_, session) =>
              session.bookingEndsAt
                ? formatEventDateTime(session.bookingEndsAt, session.timeZoneId)
                : 'At start',
          },
          {
            title: 'Venue',
            key: 'venue',
            render: (_, session) =>
              venueLabel(session) ?? <Typography.Text type="secondary">Not set</Typography.Text>,
          },
          {
            title: 'Seat map',
            key: 'seatMap',
            render: (_, session) =>
              session.seatMapVersionNumber == null ? (
                <Typography.Text type="secondary">Not attached</Typography.Text>
              ) : (
                <Tag>v{session.seatMapVersionNumber}</Tag>
              ),
          },
          {
            // The number a publish is refused over: a block with no ticket type is capacity
            // Inventory never hears about, so the map would render with a hole nobody can tell
            // from a sold-out block.
            title: 'Allocated blocks',
            key: 'allocations',
            render: (_, session) =>
              session.allocations.length === 0 ? (
                <Typography.Text type="warning">None</Typography.Text>
              ) : (
                session.allocations.length
              ),
          },
          {
            title: 'Status',
            key: 'status',
            render: (_, session) => (
              <Space size={6}>
                <Tag color={SESSION_STATUS_COLOR[session.status]}>{session.status}</Tag>
                {session.salesPaused && <Tag color="warning">Paused</Tag>}
              </Space>
            ),
          },
        ]}
      />
    </div>
  );
}
