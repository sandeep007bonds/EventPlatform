import { useEffect, useState } from 'react';
import { Button, Table, Tag, Typography } from 'antd';
import dayjs from 'dayjs';
import { Link, useNavigate } from 'react-router-dom';
import { listEvents, type EventResponse } from '../../../services/catalog/catalogApi';
import { TableSkeleton } from '../../../components/common/skeletons/TableSkeleton';
import { eventStatusColor } from '../../../utils/eventStatus';
import { toast } from '../../../components/common/feedback/toast';

const PAGE_SIZE = 20;

/** The organizer's own events, at any status — not the public browse list. */
export function AdminEventsPage() {
  const navigate = useNavigate();
  const [events, setEvents] = useState<EventResponse[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    listEvents({ mine: true, page, pageSize: PAGE_SIZE })
      .then((result) => {
        if (cancelled) {
          return;
        }
        setEvents(result.events);
        setTotalCount(result.totalCount);
      })
      .catch(() => toast.error('Could not load your events.'))
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [page]);

  if (loading) {
    return <TableSkeleton />;
  }

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Events
        </Typography.Title>
        <Link to="/admin/events/new">
          <Button type="primary">Create event</Button>
        </Link>
      </div>
      <Table<EventResponse>
        rowKey="id"
        dataSource={events}
        pagination={{
          current: page,
          pageSize: PAGE_SIZE,
          total: totalCount,
          onChange: setPage,
          showSizeChanger: false,
        }}
        onRow={(record) => ({
          onClick: () => void navigate(`/admin/events/${record.id}`),
          style: { cursor: 'pointer' },
        })}
        columns={[
          { title: 'Title', dataIndex: 'title' },
          {
            title: 'Status',
            dataIndex: 'status',
            render: (status: EventResponse['status']) => (
              <Tag color={eventStatusColor[status]}>{status}</Tag>
            ),
          },
          {
            title: 'Starts',
            dataIndex: 'startsAt',
            render: (startsAt: string) => dayjs(startsAt).format('MMM D, YYYY · h:mm A'),
          },
        ]}
      />
    </>
  );
}
