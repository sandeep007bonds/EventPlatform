import { useEffect, useState } from 'react';
import { Button, Card, Descriptions, Tag, Typography } from 'antd';
import dayjs from 'dayjs';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
  getEvent,
  getSeatMap,
  getVenue,
  type EventResponse,
  type SeatMapResponse,
  type VenueResponse,
} from '../../../services/catalog/catalogApi';
import { getInventoryCount } from '../../../services/inventory/inventoryApi';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { NotFoundPage } from '../../../components/common/errors/NotFoundPage';
import { eventStatusColor } from '../../../utils/eventStatus';
import { toEmbedUrl } from '../../../utils/videoEmbed';
import { useAuth } from '../../../contexts/useAuth';
import { toast } from '../../../components/common/feedback/toast';

/** Public event detail page — no login required to view (see ADR-0015). */
export function EventDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const navigate = useNavigate();

  const [event, setEvent] = useState<EventResponse | null>(null);
  const [seatMap, setSeatMap] = useState<SeatMapResponse | null>(null);
  const [venue, setVenue] = useState<VenueResponse | null>(null);
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

        // Sequenced after the event resolves, since it needs event.venueId — kept out of the
        // Promise.all above (seat map/inventory only need the route param, venue doesn't).
        if (eventResult.venueId) {
          getVenue(eventResult.venueId)
            .then((venueResult) => {
              if (!cancelled) {
                setVenue(venueResult);
              }
            })
            .catch(() => {
              if (!cancelled) {
                setVenue(null);
              }
            });
        }
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

  const embedUrl = event.videoUrl ? toEmbedUrl(event.videoUrl) : null;

  return (
    <Card>
      {event.bannerImageUrl && (
        <img
          src={event.bannerImageUrl}
          alt={event.title}
          style={{
            width: '100%',
            borderRadius: 8,
            marginBottom: 16,
            maxHeight: 360,
            objectFit: 'cover',
          }}
        />
      )}
      <Typography.Title level={2}>{event.title}</Typography.Title>
      <Tag color={eventStatusColor[event.status]}>{event.status}</Tag>
      {event.category && <Tag>{event.category}</Tag>}
      <Descriptions column={1} style={{ marginTop: 24 }}>
        <Descriptions.Item label="Date">
          {dayjs(event.startsAt).format('dddd, MMMM D, YYYY · h:mm A')}
        </Descriptions.Item>
        {event.doorsOpenAt && (
          <Descriptions.Item label="Doors open">
            {dayjs(event.doorsOpenAt).format('h:mm A')}
          </Descriptions.Item>
        )}
        {event.endsAt && (
          <Descriptions.Item label="Ends">{dayjs(event.endsAt).format('h:mm A')}</Descriptions.Item>
        )}
        {venue && (
          <Descriptions.Item label="Venue">
            {venue.name} — {venue.city}
          </Descriptions.Item>
        )}
        <Descriptions.Item label="Currency">{event.currency}</Descriptions.Item>
        {event.ageRestriction && (
          <Descriptions.Item label="Age restriction">{event.ageRestriction}</Descriptions.Item>
        )}
        {seatMap && (
          <Descriptions.Item label="Venue capacity">{seatMap.capacity} seats</Descriptions.Item>
        )}
        {availableCount !== null && (
          <Descriptions.Item label="Seats provisioned">{availableCount}</Descriptions.Item>
        )}
      </Descriptions>
      {event.description && (
        <Typography.Paragraph style={{ marginTop: 16 }}>{event.description}</Typography.Paragraph>
      )}
      {event.videoUrl &&
        (embedUrl ? (
          <div style={{ position: 'relative', paddingTop: '56.25%', marginTop: 16 }}>
            <iframe
              src={embedUrl}
              title={`${event.title} video`}
              allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
              allowFullScreen
              style={{
                position: 'absolute',
                top: 0,
                left: 0,
                width: '100%',
                height: '100%',
                border: 0,
              }}
            />
          </div>
        ) : (
          <Typography.Link href={event.videoUrl} target="_blank" rel="noreferrer">
            Watch trailer ↗
          </Typography.Link>
        ))}
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
