import { useEffect, useMemo, useState } from 'react';
import { Button, Card, Col, Descriptions, Radio, Row, Space, Tag, Typography, Divider } from 'antd';
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
  getEventBySlug,
  getEventGroup,
  listEvents,
  type EventGroupResponse,
  type EventResponse,
  type EventSessionResponse,
} from '../../../services/catalog/catalogApi';
import { getSeatMap, type SeatMapResponse } from '../../../services/venue/venueApi';
import { getInventoryCount } from '../../../services/inventory/inventoryApi';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { NotFoundPage } from '../../../components/common/errors/NotFoundPage';
import { ServerErrorPage } from '../../../components/common/errors/ServerErrorPage';
import { eventStatusColor } from '../../../utils/eventStatus';
import {
  inStartOrder,
  isSellable,
  primarySession,
  runLabel,
  sessionLabel,
  upcomingSellableSessions,
  venueLabel,
} from '../../../utils/eventSessions';
import { toEmbedUrl } from '../../../utils/videoEmbed';
import { toast } from '../../../components/common/feedback/toast';
import { EventPoliciesSection } from './EventPoliciesSection';

/**
 * Public event detail page — no login required to view (see ADR-0015).
 *
 * The route param is either the event's GUID or its slug. Both resolve to the same page: links
 * issued before slugs existed keep working, and everything the platform hands out from now on is
 * the readable one.
 *
 * Since ADR-0039 an event is a **run of performances**, so this page has to answer "which night?"
 * before it can show a time, a venue or a Select-seats button. It picks the next one on sale and
 * lets the buyer change it; a single-performance event renders exactly as it did before, with no
 * picker and no extra step.
 */
export function EventDetailPage() {
  const { eventSlug } = useParams<{ eventSlug: string }>();
  const navigate = useNavigate();

  const [event, setEvent] = useState<EventResponse | null>(null);
  const [selectedSessionId, setSelectedSessionId] = useState<string | null>(null);
  // Tagged with the performance it belongs to rather than cleared on switch: a bare `setSeatMap`
  // in the effect body is both a cascading render and a window in which the previous night's
  // capacity is shown under the new night's heading.
  const [sessionDetail, setSessionDetail] = useState<{
    eventSessionId: string;
    seatMap: SeatMapResponse | null;
    availableCount: number | null;
  } | null>(null);
  const [eventGroup, setEventGroup] = useState<EventGroupResponse | null>(null);
  const [otherLegs, setOtherLegs] = useState<EventResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [loadError, setLoadError] = useState(false);

  useEffect(() => {
    if (!eventSlug) {
      return;
    }

    let cancelled = false;

    // The event resolves first, then everything keyed on its id — a slug in the URL means the id is
    // not known until the first call returns, so these cannot all start together.
    (isGuid(eventSlug) ? getEvent(eventSlug) : getEventBySlug(eventSlug))
      .then((eventResult) => {
        if (cancelled) {
          return;
        }

        setEvent(eventResult);

        // The next performance still on sale, or — when none is — the one a summary would speak
        // for, so a sold-out or finished run still shows its details rather than a blank page.
        const sellable = upcomingSellableSessions(eventResult);
        setSelectedSessionId((sellable[0] ?? primarySession(eventResult))?.id ?? null);

        // Sequenced after the event resolves, since it needs event.eventGroupId.
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
  }, [eventSlug]);

  const session = useMemo<EventSessionResponse | null>(
    () => event?.sessions.find((candidate) => candidate.id === selectedSessionId) ?? null,
    [event, selectedSessionId],
  );

  // The seat map and the availability count both belong to the *selected* performance, so they are
  // re-fetched whenever the buyer switches night rather than loaded once with the event.
  useEffect(() => {
    if (!session?.seatMapId) {
      return;
    }

    let cancelled = false;
    const { seatMapId, seatMapVersionNumber, id: eventSessionId } = session;

    void Promise.all([
      getSeatMap(seatMapId, seatMapVersionNumber ?? undefined).catch(() => null),
      getInventoryCount(eventSessionId).catch(() => null),
    ]).then(([seatMapResult, inventoryResult]) => {
      if (cancelled) {
        return;
      }
      setSessionDetail({
        eventSessionId,
        seatMap: seatMapResult,
        availableCount: inventoryResult?.seatCount ?? null,
      });
    });

    return () => {
      cancelled = true;
    };
  }, [session]);

  // Only ever the selected performance's own detail; anything left over from the last one reads as
  // "not loaded yet", which is the truth.
  const detail = sessionDetail?.eventSessionId === session?.id ? sessionDetail : null;
  const seatMap = detail?.seatMap ?? null;
  const availableCount = detail?.availableCount ?? null;

  const handleSelectSeats = () => {
    // No login gate here — a buyer picks seats freely and only verifies via OTP when they
    // actually hold them (see SeatSelectionPage.tsx's handleHold, ADR-0016).
    if (!event || !session) {
      return;
    }
    if (!seatMap) {
      toast.error('This performance has no seat map yet.');
      return;
    }
    if (event.onSaleAt && dayjs(event.onSaleAt).isAfter(dayjs())) {
      toast.error('Tickets are not on sale yet for this event.');
      return;
    }
    if (session.salesPaused) {
      toast.error('Sales are currently paused for this performance.');
      return;
    }

    // The waiting room gates the on-sale, which covers the whole run — so it is keyed on the
    // event, and the buyer picks their night on the way out of it, not on the way in.
    if (event.requiresQueue) {
      void navigate(`/events/${event.slug}/queue?eventSessionId=${session.id}`);
      return;
    }
    void navigate(`/events/${event.slug}/s/${session.id}/seats`);
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

  // Every one of these is a property of the chosen performance, not of the run: booking closes at a
  // different instant every night, and one night can be paused while the others sell.
  const bookingClosed =
    session?.bookingEndsAt != null && dayjs(session.bookingEndsAt).isBefore(dayjs());
  const notOnSaleYet = event.onSaleAt != null && dayjs(event.onSaleAt).isAfter(dayjs());
  const sellableSessions = upcomingSellableSessions(event);
  const zoneLabel = session ? eventZoneAbbreviation(session.startsAt, session.timeZoneId) : null;

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
        {event.sessions.length > 1 && <Tag>{event.sessions.length} performances</Tag>}
      </Space>

      <Row gutter={32}>
        <Col xs={24} lg={15}>
          <Card styles={{ body: { padding: 24 } }}>
            <Space direction="vertical" size={10} style={{ width: '100%' }}>
              <Space>
                <CalendarOutlined style={{ color: '#3ea8c4' }} />
                <Typography.Text>
                  {session
                    ? formatEventDateTimeLong(session.startsAt, session.timeZoneId)
                    : (runLabel(event) ?? 'Dates to be announced')}
                  {session?.doorsOpenAt &&
                    ` · Doors ${formatEventTime(session.doorsOpenAt, session.timeZoneId)}`}
                  {zoneLabel && ` (${zoneLabel})`}
                </Typography.Text>
              </Space>
              <Space align="start">
                <EnvironmentOutlined style={{ color: '#3ea8c4' }} />
                <Typography.Text>{venueLabel(session) ?? 'Venue to be announced'}</Typography.Text>
              </Space>
              {notOnSaleYet && event.onSaleAt && (
                <Tag color="blue" style={{ width: 'fit-content' }}>
                  On sale {formatEventDateTime(event.onSaleAt, session?.timeZoneId)}
                </Tag>
              )}
              {session?.salesPaused && (
                <Tag color="warning" style={{ width: 'fit-content' }}>
                  Sales paused
                </Tag>
              )}
              {bookingClosed && (
                <Tag color="warning" style={{ width: 'fit-content' }}>
                  Booking closed
                </Tag>
              )}
              {!bookingClosed && session?.bookingEndsAt && (
                <Space>
                  <ClockCircleOutlined style={{ color: '#faad14' }} />
                  <Typography.Text type="warning">
                    Booking closes {formatEventDateTime(session.bookingEndsAt, session.timeZoneId)}
                  </Typography.Text>
                </Space>
              )}
              {event.maxTicketsPerBuyer != null && (
                <Typography.Text type="secondary">
                  Limit: {event.maxTicketsPerBuyer} per person
                  {event.sessions.length > 1 && ', across every performance'}
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
                      {seatMap.version.capacity} total
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
                  Other cities on this tour
                </Typography.Title>
                <Space direction="vertical" size={4}>
                  {otherLegs.map((leg) => {
                    const legSession = primarySession(leg);
                    return (
                      <Link key={leg.id} to={`/events/${leg.slug}`}>
                        {leg.firstSessionStartsAt
                          ? formatEventDate(leg.firstSessionStartsAt, legSession?.timeZoneId)
                          : 'Dates TBA'}{' '}
                        — {venueLabel(legSession) ?? leg.title}
                      </Link>
                    );
                  })}
                </Space>
              </>
            )}

            <EventPoliciesSection eventId={event.id} />
          </Card>
        </Col>

        <Col xs={24} lg={9}>
          <Card style={{ position: 'sticky', top: 88 }} styles={{ body: { padding: 24 } }}>
            <Typography.Title level={5} style={{ marginTop: 0 }}>
              Get your tickets
            </Typography.Title>

            {/*
              Only shown when there is a choice to make. One performance is the common case and
              should look like it always did — an extra radio group with a single option is a
              question the buyer does not need to be asked.
            */}
            {sellableSessions.length > 1 ? (
              <>
                <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 8 }}>
                  Choose a performance
                </Typography.Text>
                <Radio.Group
                  value={selectedSessionId}
                  onChange={(changed) => setSelectedSessionId(changed.target.value as string)}
                  style={{ display: 'block', marginBottom: 20 }}
                >
                  <Space direction="vertical" size={6} style={{ width: '100%' }}>
                    {sellableSessions.map((candidate) => (
                      <Radio key={candidate.id} value={candidate.id}>
                        {sessionLabel(candidate)}
                      </Radio>
                    ))}
                  </Space>
                </Radio.Group>
              </>
            ) : (
              <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 20 }}>
                {session ? sessionLabel(session) : (runLabel(event) ?? 'Dates to be announced')} ·{' '}
                {event.currency}
              </Typography.Text>
            )}

            {session && seatMap ? (
              <Button
                type="primary"
                size="large"
                block
                disabled={bookingClosed || notOnSaleYet || !isSellable(session)}
                onClick={handleSelectSeats}
              >
                {bookingClosed
                  ? 'Booking closed'
                  : notOnSaleYet
                    ? 'Not on sale yet'
                    : 'Select seats'}
              </Button>
            ) : (
              <Typography.Text type="secondary">
                {inStartOrder(event.sessions).length === 0
                  ? 'No performances have been scheduled yet.'
                  : "Seats aren't on sale yet."}
              </Typography.Text>
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

/**
 * Whether the route param is an event id rather than a slug.
 *
 * A slug can never look like this: `EventSlug` refuses anything with two adjacent hyphens, which is
 * every GUID in this shape. So the check is unambiguous in both directions, not just a heuristic.
 */
function isGuid(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);
}
