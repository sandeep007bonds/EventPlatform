import { useEffect, useState } from 'react';
import { Card, Col, Empty, Input, Pagination, Row, Select, Tag, Typography } from 'antd';
import { EnvironmentOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { Link } from 'react-router-dom';
import {
  listEvents,
  type EventResponse,
  type EventStatus,
} from '../../../services/catalog/catalogApi';
import { CardSkeleton } from '../../../components/common/skeletons/CardSkeleton';
import { PageHeader } from '../../../components/common/layout/PageHeader';
import { Toolbar } from '../../../components/common/layout/Toolbar';
import { eventStatusColor } from '../../../utils/eventStatus';
import { toast } from '../../../components/common/feedback/toast';

const PAGE_SIZE = 12;
const STATUS_OPTIONS: EventStatus[] = ['Published', 'OnSale', 'SoldOut', 'Cancelled', 'Completed'];

/** Public event browse page — no login required (see ADR-0015). */
export function EventsListPage() {
  const [events, setEvents] = useState<EventResponse[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<EventStatus | undefined>(undefined);
  const [titleFilter, setTitleFilter] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    listEvents({ status, page, pageSize: PAGE_SIZE })
      .then((result) => {
        if (cancelled) {
          return;
        }
        setEvents(result.events);
        setTotalCount(result.totalCount);
      })
      .catch(() => toast.error('Could not load events.'))
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [page, status]);

  const header = (
    <PageHeader
      title="Discover live events"
      description="Browse what's on sale and grab your tickets before they're gone."
    />
  );

  const toolbar = (
    <Toolbar>
      <Input.Search
        placeholder="Search by title"
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
  );

  if (loading) {
    return (
      <>
        {header}
        {toolbar}
        <Row gutter={[20, 20]}>
          {Array.from({ length: 6 }, (_, index) => (
            <Col key={index} xs={24} sm={12} lg={8}>
              <CardSkeleton />
            </Col>
          ))}
        </Row>
      </>
    );
  }

  const visibleEvents = titleFilter
    ? events.filter((event) => event.title.toLowerCase().includes(titleFilter.toLowerCase()))
    : events;

  return (
    <>
      {header}
      {toolbar}
      {visibleEvents.length === 0 ? (
        <Empty description="No events found" style={{ margin: '64px 0' }} />
      ) : (
        <Row gutter={[20, 20]}>
          {visibleEvents.map((event) => (
            <Col key={event.id} xs={24} sm={12} lg={8}>
              <Link to={`/events/${event.id}`} style={{ display: 'block', height: '100%' }}>
                <Card
                  hoverable
                  styles={{ body: { padding: 18 } }}
                  style={{ height: '100%', overflow: 'hidden' }}
                  cover={
                    event.bannerImageUrl ? (
                      <div style={{ height: 168, overflow: 'hidden' }}>
                        <img
                          src={event.bannerImageUrl}
                          alt={event.title}
                          style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                        />
                      </div>
                    ) : (
                      <div
                        style={{
                          height: 168,
                          background:
                            'linear-gradient(135deg, rgba(62,168,196,0.35), rgba(28,43,48,0.65))',
                        }}
                      />
                    )
                  }
                >
                  <Tag color={eventStatusColor[event.status]} style={{ marginBottom: 8 }}>
                    {event.status}
                  </Tag>
                  <Typography.Title
                    level={5}
                    style={{ margin: 0, lineHeight: 1.3 }}
                    ellipsis={{ rows: 2 }}
                  >
                    {event.title}
                  </Typography.Title>
                  <Typography.Text type="secondary" style={{ display: 'block', marginTop: 8 }}>
                    {dayjs(event.startsAt).format('ddd, MMM D, YYYY · h:mm A')}
                  </Typography.Text>
                  <Typography.Text type="secondary" style={{ display: 'block', marginTop: 2 }}>
                    <EnvironmentOutlined style={{ marginRight: 6 }} />
                    {event.city}
                  </Typography.Text>
                </Card>
              </Link>
            </Col>
          ))}
        </Row>
      )}
      <Pagination
        style={{ marginTop: 32, textAlign: 'center' }}
        current={page}
        pageSize={PAGE_SIZE}
        total={totalCount}
        onChange={setPage}
        showSizeChanger={false}
      />
    </>
  );
}
