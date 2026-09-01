import { useEffect, useState } from 'react';
import { Button, Card, Col, Descriptions, Row, Space, Tag, Typography, Divider } from 'antd';
import type { AxiosError } from 'axios';
import {
  CalendarOutlined,
  ClockCircleOutlined,
  EnvironmentOutlined,
  GlobalOutlined,
  MailOutlined,
  PhoneOutlined,
} from '@ant-design/icons';
import dayjs from 'dayjs';
import {
  eventZoneAbbreviation,
  formatEventDate,
  formatEventDateTime,
  formatEventDateTimeLong,
  formatEventTime,
} from '../../../utils/eventTime';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
  getEvent,
  getEventGroup,
  getSeatMap,
  listEvents,
  type EventGroupResponse,
  type EventResponse,
  type SeatMapResponse,
} from '../../../services/catalog/catalogApi';
import { getInventoryCount } from '../../../services/inventory/inventoryApi';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { NotFoundPage } from '../../../components/common/errors/NotFoundPage';
import { ServerErrorPage } from '../../../components/common/errors/ServerErrorPage';
import { eventStatusColor } from '../../../utils/eventStatus';
import { toEmbedUrl } from '../../../utils/videoEmbed';
import { toast } from '../../../components/common/feedback/toast';

/** Public event detail page — no login required to view (see ADR-0015). */
export function EventDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [event, setEvent] = useState<EventResponse | null>(null);
  const [seatMap, setSeatMap] = useState<SeatMapResponse | null>(null);
  const [eventGroup, setEventGroup] = useState<EventGroupResponse | null>(null);
  const [otherLegs, setOtherLegs] = useState<EventResponse[]>([]);
  const [availableCount, setAvailableCount] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [loadError, setLoadError] = useState(false);

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

        // Sequenced after the event resolves, since it needs event.eventGroupId — kept out of
        // the Promise.all above (seat map/inventory only need the route param, the tour doesn't).
        if (eventResult.eventGroupId) {
          const groupId = eventResult.eventGroupId;

          getEventGroup(groupId)
            .then((groupResult) => {
              if (!cancelled) {
                setEventGroup(groupResult);
              }
            })
            .catch(() => {
              if (!cancelled) {
                setEventGroup(null);
              }
            });

          listEvents({ eventGroupId: groupId, pageSize: 50 })
            .then((result) => {
              if (!cancelled) {
                setOtherLegs(result.events.filter((leg) => leg.id !== eventResult.id));
              }
            })
            .catch(() => {
              if (!cancelled) {
                setOtherLegs([]);
              }
            });
        }
      })
      .catch((error: AxiosError) => {
        if (error.response?.status === 404) {
          setNotFound(true);
        } else {
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
  }, [id]);

  const handleSelectSeats = () => {
    // No login gate here — a buyer picks seats freely and only verifies via OTP when they
    // actually hold them (see SeatSelectionPage.tsx's handleHold, ADR-0016).
    if (!seatMap) {
      toast.error('This event has no seat map yet.');
      return;
    }
    if (event?.onSaleAt && dayjs(event.onSaleAt).isAfter(dayjs())) {
      toast.error('Tickets are not on sale yet for this event.');
      return;
    }
    if (event?.salesPaused) {
      toast.error('Sales are currently paused for this event.');
      return;
    }
    if (event?.requiresQueue) {
      void navigate(`/events/${id}/queue`);
      return;
    }
    void navigate(`/events/${id}/seats`);
  };

  if (loading) {
    return <DetailSkeleton />;
  }

  if (loadError) {
    return <ServerErrorPage />;
  }

  if (notFound || !event) {
    return <NotFoundPage />;
  }

  const embedUrl = event.videoUrl ? toEmbedUrl(event.videoUrl) : null;
  const hasContact =
    event.contactPhone ??
    event.contactMobile ??
    event.contactEmail ??
    event.websiteUrl ??
    event.socialLinks.length > 0;
  const bookingClosed =
    event.bookingEndsAt !== null && dayjs(event.bookingEndsAt).isBefore(dayjs());
  const notOnSaleYet = event.onSaleAt !== null && dayjs(event.onSaleAt).isAfter(dayjs());

  // Named only when the event has a zone; without one the times are already in the reader's own.
  const zoneLabel = eventZoneAbbreviation(event.startsAt, event.timeZoneId);

  return (
    <div>
      {event.bannerImageUrl ? (
        <div
          style={{
            height: 320,
            borderRadius: 16,
            overflow: 'hidden',
            marginBottom: 24,
          }}
        >
          <img
            src={event.bannerImageUrl}
            alt={event.title}
            style={{ width: '100%', height: '100%', objectFit: 'cover' }}
          />
        </div>
      ) : (
        <div
          style={{
            height: 200,
            borderRadius: 16,
            marginBottom: 24,
            background: 'linear-gradient(135deg, rgba(62,168,196,0.35), rgba(28,43,48,0.75))',
          }}
        />
      )}

      {eventGroup && (
        <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 4 }}>
          Part of:{' '}
          <Typography.Text strong style={{ color: 'inherit' }}>
            {eventGroup.title}
          </Typography.Text>
        </Typography.Text>
      )}
      <Typography.Title level={2} style={{ marginTop: 0, marginBottom: 12 }}>
        {event.title}
      </Typography.Title>
      <Space size={8} wrap style={{ marginBottom: 24 }}>
        <Tag color={eventStatusColor[event.status]}>{event.status}</Tag>
        {event.category && <Tag>{event.category}</Tag>}
      </Space>

      <Row gutter={32}>
        <Col xs={24} lg={15}>
          <Card styles={{ body: { padding: 24 } }}>
            <Space direction="vertical" size={10} style={{ width: '100%' }}>
              <Space>
                <CalendarOutlined style={{ color: '#3ea8c4' }} />
                <Typography.Text>
                  {formatEventDateTimeLong(event.startsAt, event.timeZoneId)}
                  {event.doorsOpenAt &&
                    ` · Doors ${formatEventTime(event.doorsOpenAt, event.timeZoneId)}`}
                  {zoneLabel && ` (${zoneLabel})`}
                </Typography.Text>
              </Space>
              <Space align="start">
                <EnvironmentOutlined style={{ color: '#3ea8c4' }} />
                <Typography.Text>
                  {event.locationName} — {event.addressLine1}, {event.city}
                </Typography.Text>
              </Space>
              {notOnSaleYet && event.onSaleAt && (
                <Tag color="blue" style={{ width: 'fit-content' }}>
                  On sale {formatEventDateTime(event.onSaleAt, event.timeZoneId)}
                </Tag>
              )}
              {event.salesPaused && (
                <Tag color="warning" style={{ width: 'fit-content' }}>
                  Sales paused
                </Tag>
              )}
              {bookingClosed && (
                <Tag color="warning" style={{ width: 'fit-content' }}>
                  Booking closed
                </Tag>
              )}
              {!bookingClosed && event.bookingEndsAt && (
                <Space>
                  <ClockCircleOutlined style={{ color: '#faad14' }} />
                  <Typography.Text type="warning">
                    Booking closes {formatEventDateTime(event.bookingEndsAt, event.timeZoneId)}
                  </Typography.Text>
                </Space>
              )}
              {event.maxTicketsPerBuyer !== null && (
                <Typography.Text type="secondary">
                  Limit: {event.maxTicketsPerBuyer} per person
                </Typography.Text>
              )}
            </Space>

            {event.description && (
              <>
                <Divider />
                <Typography.Paragraph style={{ marginBottom: 0, whiteSpace: 'pre-line' }}>
                  {event.description}
                </Typography.Paragraph>
              </>
            )}

            {(event.ageRestriction ?? seatMap ?? availableCount !== null) && (
              <>
                <Divider />
                <Descriptions column={1} size="small">
                  {event.ageRestriction && (
                    <Descriptions.Item label="Age restriction">
                      {event.ageRestriction}
                    </Descriptions.Item>
                  )}
                  {seatMap && (
                    <Descriptions.Item label="Venue capacity">
                      {seatMap.capacity} total
                    </Descriptions.Item>
                  )}
                  {availableCount !== null && (
                    <Descriptions.Item label="Seats provisioned">
                      {availableCount}
                    </Descriptions.Item>
                  )}
                </Descriptions>
              </>
            )}

            {event.videoUrl && (
              <>
                <Divider />
                {embedUrl ? (
                  <div style={{ position: 'relative', paddingTop: '56.25%' }}>
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
                        borderRadius: 10,
                      }}
                    />
                  </div>
                ) : (
                  <Typography.Link href={event.videoUrl} target="_blank" rel="noreferrer">
                    Watch trailer ↗
                  </Typography.Link>
                )}
              </>
            )}

            {hasContact && (
              <>
                <Divider />
                <Typography.Title level={5} style={{ marginTop: 0 }}>
                  Contact
                </Typography.Title>
                <Space direction="vertical" size={6}>
                  {event.contactPhone && (
                    <Typography.Text>
                      <PhoneOutlined style={{ marginRight: 8 }} />
                      {event.contactPhone}
                    </Typography.Text>
                  )}
                  {event.contactMobile && (
                    <Typography.Text>
                      <PhoneOutlined style={{ marginRight: 8 }} />
                      {event.contactMobile}
                    </Typography.Text>
                  )}
                  {event.contactEmail && (
                    <Typography.Text>
                      <MailOutlined style={{ marginRight: 8 }} />
                      <a href={`mailto:${event.contactEmail}`}>{event.contactEmail}</a>
                    </Typography.Text>
                  )}
                  {event.websiteUrl && (
                    <Typography.Text>
                      <GlobalOutlined style={{ marginRight: 8 }} />
                      <Typography.Link href={event.websiteUrl} target="_blank" rel="noreferrer">
                        {event.websiteUrl}
                      </Typography.Link>
                    </Typography.Text>
                  )}
                  {event.socialLinks.length > 0 && (
                    <Space wrap style={{ marginTop: 4 }}>
                      {event.socialLinks.map((link) => (
                        <Tag key={link.platform + link.url} style={{ margin: 0 }}>
                          <Typography.Link href={link.url} target="_blank" rel="noreferrer">
                            {link.platform}
                          </Typography.Link>
                        </Tag>
                      ))}
                    </Space>
                  )}
                </Space>
              </>
            )}

            {otherLegs.length > 0 && (
              <>
                <Divider />
                <Typography.Title level={5} style={{ marginTop: 0 }}>
                  Other dates on this tour
                </Typography.Title>
                <Space direction="vertical" size={4}>
                  {otherLegs.map((leg) => (
                    <Link key={leg.id} to={`/events/${leg.id}`}>
                      {formatEventDate(leg.startsAt, leg.timeZoneId)} — {leg.city}
                    </Link>
                  ))}
                </Space>
              </>
            )}
          </Card>
        </Col>

        <Col xs={24} lg={9}>
          <Card style={{ position: 'sticky', top: 88 }} styles={{ body: { padding: 24 } }}>
            <Typography.Title level={5} style={{ marginTop: 0 }}>
              Get your tickets
            </Typography.Title>
            <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 20 }}>
              {formatEventDate(event.startsAt, event.timeZoneId)} · {event.currency}
            </Typography.Text>
            {seatMap ? (
              <Button
                type="primary"
                size="large"
                block
                disabled={bookingClosed || notOnSaleYet}
                onClick={handleSelectSeats}
              >
                {bookingClosed
                  ? 'Booking closed'
                  : notOnSaleYet
                    ? 'Not on sale yet'
                    : 'Select seats'}
              </Button>
            ) : (
              <Typography.Text type="secondary">Seats aren't on sale yet.</Typography.Text>
            )}
          </Card>
        </Col>
      </Row>

      <Link to="/" style={{ display: 'block', marginTop: 24 }}>
        ← Back to events
      </Link>
    </div>
  );
}
