import { useEffect, useState } from 'react';
import { Button, Input, Select, Space, Table, Tag, Typography } from 'antd';
import dayjs from 'dayjs';
import { Link, useNavigate } from 'react-router-dom';
import {
  listEvents,
  type EventResponse,
  type EventStatus,
} from '../../../services/catalog/catalogApi';
import { TableSkeleton } from '../../../components/common/skeletons/TableSkeleton';
import { eventStatusColor } from '../../../utils/eventStatus';
import { toast } from '../../../components/common/feedback/toast';

const PAGE_SIZE = 20;
const STATUS_OPTIONS: EventStatus[] = [
  'Draft',
  'Published',
  'OnSale',
  'SoldOut',
  'Cancelled',
  'Completed',
];

/** The organizer's own events, at any status — not the public browse list. */
export function AdminEventsPage() {
  const navigate = useNavigate();
  const [events, setEvents] = useState<EventResponse[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<EventStatus | undefined>(undefined);
  const [titleFilter, setTitleFilter] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    // Status is filtered server-side (Catalog supports it natively); title search is applied
    // client-side below, over the current page only — Catalog has no text-search endpoint yet.
    listEvents({ mine: true, status, page, pageSize: PAGE_SIZE })
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
  }, [page, status]);

  if (loading) {
    return <TableSkeleton />;
  }

  const visibleEvents = titleFilter
    ? events.filter((event) => event.title.toLowerCase().includes(titleFilter.toLowerCase()))
    : events;

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

      <Space style={{ marginBottom: 16 }}>
        <Input.Search
          placeholder="Search title"
          allowClear
          onChange={(event) => setTitleFilter(event.target.value)}
          style={{ width: 220 }}
        />
        <Select<EventStatus | undefined>
          placeholder="All statuses"
          allowClear
          style={{ width: 160 }}
          value={status}
          onChange={(value) => {
            setStatus(value);
            setPage(1);
          }}
          options={STATUS_OPTIONS.map((option) => ({ value: option, label: option }))}
        />
      </Space>

      <Table<EventResponse>
        rowKey="id"
        dataSource={visibleEvents}
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
          {
            title: 'Title',
            dataIndex: 'title',
            sorter: (a, b) => a.title.localeCompare(b.title),
          },
          {
            title: 'Status',
            dataIndex: 'status',
            render: (eventStatus: EventResponse['status']) => (
              <Tag color={eventStatusColor[eventStatus]}>{eventStatus}</Tag>
            ),
          },
          {
            title: 'Starts',
            dataIndex: 'startsAt',
            sorter: (a, b) => dayjs(a.startsAt).valueOf() - dayjs(b.startsAt).valueOf(),
            defaultSortOrder: 'ascend',
            render: (startsAt: string) => dayjs(startsAt).format('MMM D, YYYY · h:mm A'),
          },
        ]}
      />
    </>
  );
}
