import { useEffect, useState } from 'react';
import { Button, Card, Input, Select, Table, Tag } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { Link, useNavigate } from 'react-router-dom';
import {
  listEvents,
  type EventResponse,
  type EventStatus,
} from '../../../services/catalog/catalogApi';
import { TableSkeleton } from '../../../components/common/skeletons/TableSkeleton';
import { PageHeader } from '../../../components/common/layout/PageHeader';
import { Toolbar } from '../../../components/common/layout/Toolbar';
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
    return (
      <>
        <PageHeader title="Events" />
        <TableSkeleton />
      </>
    );
  }

  const visibleEvents = titleFilter
    ? events.filter((event) => event.title.toLowerCase().includes(titleFilter.toLowerCase()))
    : events;

  return (
    <>
      <PageHeader
        title="Events"
        description="Everything you're selling, from draft to completed."
        extra={
          <Link to="/admin/events/new">
            <Button type="primary" icon={<PlusOutlined />}>
              Create event
            </Button>
          </Link>
        }
      />

      <Toolbar>
        <Input.Search
          placeholder="Search title"
          allowClear
          onChange={(event) => setTitleFilter(event.target.value)}
          style={{ width: 240 }}
        />
        <Select<EventStatus | undefined>
          placeholder="All statuses"
          allowClear
          style={{ width: 170 }}
          value={status}
          onChange={(value) => {
            setStatus(value);
            setPage(1);
          }}
          options={STATUS_OPTIONS.map((option) => ({ value: option, label: option }))}
        />
      </Toolbar>

      <Card styles={{ body: { padding: 0 } }}>
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
              title: 'City',
              dataIndex: 'city',
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
      </Card>
    </>
  );
}
