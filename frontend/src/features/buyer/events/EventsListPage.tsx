import { useEffect, useState } from 'react';
import { Card, Col, Empty, Input, Pagination, Row, Select, Space, Tag, Typography } from 'antd';
import dayjs from 'dayjs';
import { Link } from 'react-router-dom';
import {
  listEvents,
  type EventResponse,
  type EventStatus,
} from '../../../services/catalog/catalogApi';
import { CardSkeleton } from '../../../components/common/skeletons/CardSkeleton';
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

  const filters = (
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
  );

  if (loading) {
    return (
      <>
        {filters}
        <Row gutter={[16, 16]}>
          {Array.from({ length: 6 }, (_, index) => (
            <Col key={index} xs={24} sm={12} md={8}>
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
      {filters}
      {visibleEvents.length === 0 ? (
        <Empty description="No events found" />
      ) : (
        <Row gutter={[16, 16]}>
          {visibleEvents.map((event) => (
            <Col key={event.id} xs={24} sm={12} md={8}>
              <Link to={`/events/${event.id}`}>
                <Card
                  hoverable
                  title={event.title}
                  cover={
                    event.bannerImageUrl ? (
                      <img
                        src={event.bannerImageUrl}
                        alt={event.title}
                        style={{ height: 160, objectFit: 'cover' }}
                      />
                    ) : undefined
                  }
                >
                  <Tag color={eventStatusColor[event.status]}>{event.status}</Tag>
                  <Typography.Text type="secondary" style={{ display: 'block', marginTop: 8 }}>
                    {dayjs(event.startsAt).format('MMM D, YYYY · h:mm A')}
                  </Typography.Text>
                </Card>
              </Link>
            </Col>
          ))}
        </Row>
      )}
      <Pagination
        style={{ marginTop: 24, textAlign: 'center' }}
        current={page}
        pageSize={PAGE_SIZE}
        total={totalCount}
        onChange={setPage}
        showSizeChanger={false}
      />
    </>
  );
}
