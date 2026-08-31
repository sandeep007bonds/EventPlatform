import { useEffect, useState } from 'react';
import { Button, Card, Input, Select, Tag, Typography } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { formatEventDateTime } from '../../../utils/eventTime';
import { DataGrid } from '../../../components/common/grid/DataGrid';
import { Link, useNavigate } from 'react-router-dom';
import {
  getEventGroup,
  listEvents,
  type EventResponse,
  type EventStatus,
} from '../../../services/catalog/catalogApi';
import { TableSkeleton } from '../../../components/common/skeletons/TableSkeleton';
import { PageHeader } from '../../../components/common/layout/PageHeader';
import { Toolbar } from '../../../components/common/layout/Toolbar';
import { eventStatusColor } from '../../../utils/eventStatus';
import { LoadError } from '../../../components/common/errors/LoadError';

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
  // eventGroupId -> tour title, for the "Tour" column. Catalog has no batch lookup endpoint for
  // this, so it's resolved client-side from the current page's distinct ids (at most PAGE_SIZE).
  const [tourTitles, setTourTitles] = useState<Map<string, string>>(new Map());
  const [loadError, setLoadError] = useState(false);
  const [reloadToken, setReloadToken] = useState(0);

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
        setLoadError(false);

        const groupIds = [
          ...new Set(
            result.events
              .map((event) => event.eventGroupId)
              .filter((id): id is string => id !== null),
          ),
        ];
        void Promise.all(groupIds.map((id) => getEventGroup(id).catch(() => null))).then(
          (groups) => {
            if (cancelled) {
              return;
            }
            setTourTitles(
              new Map(
                groups
                  .filter((group): group is NonNullable<typeof group> => group !== null)
                  .map((group) => [group.id, group.title]),
              ),
            );
          },
        );
      })
      .catch(() => {
        if (!cancelled) {
          setLoadError(true);
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [page, status, reloadToken]);

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

      {loadError ? (
        <Card>
          <LoadError
            description="Could not load your events."
            onRetry={() => {
              setLoading(true);
              setReloadToken((token) => token + 1);
            }}
          />
        </Card>
      ) : (
        <DataGrid<EventResponse>
          rowKey="id"
          rows={visibleEvents}
          exportFileName="events"
          countLabel={`${totalCount.toLocaleString()} event${totalCount === 1 ? '' : 's'}`}
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
              title: 'Tour',
              dataIndex: 'eventGroupId',
              exportValue: (row) =>
                row.eventGroupId ? (tourTitles.get(row.eventGroupId) ?? row.eventGroupId) : '',
              render: (eventGroupId: EventResponse['eventGroupId']) =>
                eventGroupId ? (
                  (tourTitles.get(eventGroupId) ?? '…')
                ) : (
                  <Typography.Text type="secondary">—</Typography.Text>
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
              // Exported as the ISO instant, not the rendered local string: a spreadsheet can
              // sort and filter on that, and it stays unambiguous once the file leaves here.
              exportValue: (row) => row.startsAt,
              render: (_: string, row: EventResponse) =>
                formatEventDateTime(row.startsAt, row.timeZoneId),
            },
          ]}
        />
      )}
    </>
  );
}
