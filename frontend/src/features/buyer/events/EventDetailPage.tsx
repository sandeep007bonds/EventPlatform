import { useEffect, useState } from 'react';
import { Button, Card, Descriptions, Tag, Typography } from 'antd';
import dayjs from 'dayjs';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
  getEvent,
  getSeatMap,
  type EventResponse,
  type SeatMapResponse,
} from '../../../services/catalog/catalogApi';
import { getInventoryCount } from '../../../services/inventory/inventoryApi';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { NotFoundPage } from '../../../components/common/errors/NotFoundPage';
import { eventStatusColor } from '../../../utils/eventStatus';
import { useAuth } from '../../../contexts/useAuth';
import { toast } from '../../../components/common/feedback/toast';

/** Public event detail page — no login required to view (see ADR-0015). */
export function EventDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const navigate = useNavigate();

  const [event, setEvent] = useState<EventResponse | null>(null);
  const [seatMap, setSeatMap] = useState<SeatMapResponse | null>(null);
  const [availableCount, setAvailableCount] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    if (!id) {
      return;
    }

    let cancelled = false;

    Promise.all([
      getEvent(id),
      getSeatMap(id).catch(() => null),
      getInventoryCount(id).catch(() => null),
    ])
      .then(([eventResult, seatMapResult, inventoryResult]) => {
        if (cancelled) {
          return;
        }
        setEvent(eventResult);
        setSeatMap(seatMapResult);
        setAvailableCount(inventoryResult?.seatCount ?? null);
      })
      .catch(() => setNotFound(true))
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [id]);

  const handleSelectSeats = () => {
    if (!user) {
      void navigate('/login');
      return;
    }
    if (!seatMap) {
      toast.error('This event has no seat map yet.');
      return;
    }
    void navigate(`/events/${id}/seats`);
  };

  if (loading) {
    return <DetailSkeleton />;
  }

  if (notFound || !event) {
    return <NotFoundPage />;
  }

  return (
    <Card>
      <Typography.Title level={2}>{event.title}</Typography.Title>
      <Tag color={eventStatusColor[event.status]}>{event.status}</Tag>
      <Descriptions column={1} style={{ marginTop: 24 }}>
        <Descriptions.Item label="Date">
          {dayjs(event.startsAt).format('dddd, MMMM D, YYYY · h:mm A')}
        </Descriptions.Item>
        <Descriptions.Item label="Currency">{event.currency}</Descriptions.Item>
        {seatMap && (
          <Descriptions.Item label="Venue capacity">{seatMap.capacity} seats</Descriptions.Item>
        )}
        {availableCount !== null && (
          <Descriptions.Item label="Seats provisioned">{availableCount}</Descriptions.Item>
        )}
      </Descriptions>
      {seatMap ? (
        <Button type="primary" size="large" style={{ marginTop: 16 }} onClick={handleSelectSeats}>
          Select seats
        </Button>
      ) : (
        <Typography.Text type="secondary" style={{ display: 'block', marginTop: 16 }}>
          Seats aren't on sale yet.
        </Typography.Text>
      )}
      <Link to="/" style={{ display: 'block', marginTop: 16 }}>
        ← Back to events
      </Link>
    </Card>
  );
}
