import { useEffect, useState } from 'react';
import { Card, Col, Empty, Pagination, Row, Tag, Typography } from 'antd';
import dayjs from 'dayjs';
import { Link } from 'react-router-dom';
import { listEvents, type EventResponse } from '../../../services/catalog/catalogApi';
import { CardSkeleton } from '../../../components/common/skeletons/CardSkeleton';
import { eventStatusColor } from '../../../utils/eventStatus';
import { toast } from '../../../components/common/feedback/toast';

const PAGE_SIZE = 12;

/** Public event browse page — no login required (see ADR-0015). */
export function EventsListPage() {
  const [events, setEvents] = useState<EventResponse[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    listEvents({ page, pageSize: PAGE_SIZE })
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
  }, [page]);

  if (loading) {
    return (
      <Row gutter={[16, 16]}>
        {Array.from({ length: 6 }, (_, index) => (
          <Col key={index} xs={24} sm={12} md={8}>
            <CardSkeleton />
          </Col>
        ))}
      </Row>
    );
  }

  if (events.length === 0) {
    return <Empty description="No events yet" />;
  }

  return (
    <>
      <Row gutter={[16, 16]}>
        {events.map((event) => (
          <Col key={event.id} xs={24} sm={12} md={8}>
            <Link to={`/events/${event.id}`}>
              <Card hoverable title={event.title}>
                <Tag color={eventStatusColor[event.status]}>{event.status}</Tag>
                <Typography.Text type="secondary" style={{ display: 'block', marginTop: 8 }}>
                  {dayjs(event.startsAt).format('MMM D, YYYY · h:mm A')}
                </Typography.Text>
              </Card>
            </Link>
          </Col>
        ))}
      </Row>
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
