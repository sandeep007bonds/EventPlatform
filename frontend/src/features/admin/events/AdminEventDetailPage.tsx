import { useEffect, useState } from 'react';
import { Button, Card, Descriptions, Space, Tabs, Tag, Typography } from 'antd';
import type { AxiosError } from 'axios';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { formatEventDateTime } from '../../../utils/eventTime';
import {
  getEvent,
  getSeatMap,
  listEntryGates,
  pauseSales,
  publishEvent,
  resumeSales,
  type EntryGateResponse,
  type EventResponse,
  type SeatMapResponse,
} from '../../../services/catalog/catalogApi';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { NotFoundPage } from '../../../components/common/errors/NotFoundPage';
import { ServerErrorPage } from '../../../components/common/errors/ServerErrorPage';
import { PageHeader } from '../../../components/common/layout/PageHeader';
import { PageShell } from '../../../components/common/layout/PageShell';
import { ScrollRegion } from '../../../components/common/layout/ScrollRegion';
import { PageContainer } from '../../../components/common/layout/PageContainer';
import { eventStatusColor } from '../../../utils/eventStatus';
import { toast } from '../../../components/common/feedback/toast';
import { formatMoney } from '../../../utils/money';
import { SeatBlockPanel } from '../inventory/SeatBlockPanel';
import { TourLegsList } from '../eventGroups/TourLegsList';
import { EntryGatesPanel } from './EntryGatesPanel';
import { QueueSettingsPanel } from './QueueSettingsPanel';
import { PromoCodesPanel } from '../promoCodes/PromoCodesPanel';
import { TicketTypesPanel } from '../ticketTypes/TicketTypesPanel';
import { PolicyDocumentsPanel } from '../policies/PolicyDocumentsPanel';
import { EventPresentationForm } from './EventPresentationForm';
import { EventScheduleForm } from './EventScheduleForm';
import { EventSeatMapPanel } from './EventSeatMapPanel';
import { EventSlugCard } from './EventSlugCard';

const TAB_QUERY_PARAM = 'tab';

/**
 * Organizer's event workspace.
 *
 * Grouped into tabs rather than one long scroll, because the sections have genuinely different
 * lifecycles: the event page stays editable forever, the schedule locks at publish, the seat map is
 * a pre-publish activity, and seat blocking only exists afterwards. One page made that look like
 * one decision. **Each tab saves on its own** — a tab is a unit of work, and a single Save spanning
 * "the title" and "the tax rate" would have to fail as a whole when only one half is still allowed.
 *
 * The active tab lives in the query string so a reload, a bookmark or a link to "the policies of
 * this event" all land where they should.
 */
export function AdminEventDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();

  const [event, setEvent] = useState<EventResponse | null>(null);
  const [seatMap, setSeatMap] = useState<SeatMapResponse | null>(null);
  const [entryGates, setEntryGates] = useState<EntryGateResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [loadError, setLoadError] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [togglingSales, setTogglingSales] = useState(false);

  const load = (eventId: string) => {
    Promise.all([
      getEvent(eventId),
      getSeatMap(eventId).catch(() => null),
      listEntryGates(eventId).catch(() => []),
    ])
      .then(([eventResult, seatMapResult, gatesResult]) => {
        setEvent(eventResult);
        setSeatMap(seatMapResult);
        setEntryGates(gatesResult);
        setNotFound(false);
        setLoadError(false);
      })
      .catch((error: AxiosError) => {
        if (error.response?.status === 404) {
          setNotFound(true);
        } else {
          setLoadError(true);
        }
      })
      .finally(() => setLoading(false));
  };

  const refreshEntryGates = () => {
    if (id) {
      listEntryGates(id)
        .then(setEntryGates)
        .catch(() => toast.error('Could not load entry gates.'));
    }
  };

  useEffect(() => {
    if (id) {
      load(id);
    }
  }, [id]);

  const handlePublish = async () => {
    if (!id) {
      return;
    }
    setSubmitting(true);
    try {
      await publishEvent(id);
      toast.success('Event published.');
      load(id);
    } catch {
      toast.error('Could not publish this event — check it has a seat map and is still a draft.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleToggleSales = async () => {
    if (!id || !event) {
      return;
    }
    setTogglingSales(true);
    try {
      if (event.salesPaused) {
        await resumeSales(id);
        toast.success('Sales resumed.');
      } else {
        await pauseSales(id);
        toast.success('Sales paused.');
      }
      load(id);
    } catch {
      toast.error('Could not update sales status for this event.');
    } finally {
      setTogglingSales(false);
    }
  };

  if (loading) {
    return (
      <PageShell>
        <DetailSkeleton />
      </PageShell>
    );
  }

  if (loadError) {
    return (
      <PageShell>
        <ServerErrorPage />
      </PageShell>
    );
  }

  if (notFound || !event || !id) {
    return (
      <PageShell>
        <NotFoundPage />
      </PageShell>
    );
  }

  const isDraft = event.status === 'Draft';
  const reload = () => load(id);

  return (
    <>
      {/* Pinned: identity and navigation, which are worth their vertical cost because you need
          them at every scroll position. Everything below scrolls under it. */}
      <div style={{ padding: '28px 32px 0', flex: '0 0 auto' }}>
        <PageContainer maxWidth={1360}>
          <PageHeader
            title={
              <Space align="center">
                {event.title}
                <Tag color={eventStatusColor[event.status]} style={{ marginLeft: 4 }}>
                  {event.status}
                </Tag>
                {event.salesPaused && <Tag color="warning">Sales paused</Tag>}
              </Space>
            }
            extra={
              <>
                {isDraft && seatMap && (
                  <Button type="primary" loading={submitting} onClick={() => void handlePublish()}>
                    Publish
                  </Button>
                )}
                {event.status === 'Published' && (
                  <Button
                    danger={!event.salesPaused}
                    loading={togglingSales}
                    onClick={() => void handleToggleSales()}
                  >
                    {event.salesPaused ? 'Resume sales' : 'Pause sales'}
                  </Button>
                )}
                <Button onClick={() => void navigate('/admin')}>← Back to events</Button>
              </>
            }
          />

          <Card style={{ marginBottom: 24 }}>
            <Descriptions column={{ xs: 1, sm: 2, md: 4 }} size="small">
              <Descriptions.Item label="Starts">
                {formatEventDateTime(event.startsAt, event.timeZoneId)}
              </Descriptions.Item>
              <Descriptions.Item label="Ends">
                {formatEventDateTime(event.endsAt, event.timeZoneId)}
              </Descriptions.Item>
              <Descriptions.Item label="Venue">
                {event.locationName}, {event.city}
              </Descriptions.Item>
              <Descriptions.Item label="Time zone">
                {event.timeZoneId ?? 'Not set'}
              </Descriptions.Item>
              <Descriptions.Item label="Currency">{event.currency}</Descriptions.Item>
              <Descriptions.Item label="Tax">
                {event.taxRatePercent ? (event.taxLabel ?? `${event.taxRatePercent}%`) : 'None'}
              </Descriptions.Item>
              <Descriptions.Item label="Booking fee">
                {event.bookingFeePerTicketMinor
                  ? `${formatMoney(event.bookingFeePerTicketMinor, event.currency)} per ticket`
                  : 'None'}
              </Descriptions.Item>
              <Descriptions.Item label="Capacity">
                {seatMap ? `${seatMap.capacity} total` : '—'}
              </Descriptions.Item>
            </Descriptions>
            {isDraft && !seatMap && (
              <Typography.Text type="warning" style={{ display: 'block', marginTop: 12 }}>
                Define a seat map on the Seat map tab before you can publish.
              </Typography.Text>
            )}
          </Card>
        </PageContainer>
      </div>

      <ScrollRegion padding="0 32px 0">
        <PageContainer maxWidth={1360}>
          {event.eventGroupId && (
            <Card
              title="Part of a tour"
              style={{ marginBottom: 24 }}
              styles={{ body: { padding: 28 } }}
            >
              <TourLegsList eventGroupId={event.eventGroupId} excludeEventId={event.id} />
            </Card>
          )}

          <Card styles={{ body: { padding: 28 } }}>
            <Tabs
              // Pinning the tab row this way rather than lifting it into the block above: the nav
              // and its panels are one component, and `renderTabBar` is the supported seam for
              // exactly this. Sticky also means no measuring — it needs no knowledge of how tall
              // the header happens to be today.
              renderTabBar={(tabBarProps, DefaultTabBar) => (
                <div
                  style={{
                    position: 'sticky',
                    top: 0,
                    zIndex: 3,
                    margin: '-28px -28px 0',
                    padding: '28px 28px 0',
                    background: 'inherit',
                  }}
                >
                  <DefaultTabBar {...tabBarProps} />
                </div>
              )}
              activeKey={searchParams.get(TAB_QUERY_PARAM) ?? 'page'}
              onChange={(key) => setSearchParams({ [TAB_QUERY_PARAM]: key }, { replace: true })}
              items={[
                {
                  key: 'page',
                  label: 'Event page',
                  children: <EventPresentationForm event={event} onSaved={reload} />,
                },
                {
                  key: 'schedule',
                  label: (
                    <Space size={6}>
                      Schedule &amp; venue
                      {!isDraft && <Tag>Locked</Tag>}
                    </Space>
                  ),
                  children: <EventScheduleForm event={event} onSaved={reload} />,
                },
                {
                  key: 'tickets',
                  label: 'Tickets & pricing',
                  children: (
                    <>
                      <TicketTypesPanel eventId={id} currency={event.currency} isDraft={isDraft} />
                      <PromoCodesPanel
                        eventId={id}
                        currency={event.currency}
                        priceTiers={[
                          ...new Set([
                            ...(seatMap?.seats.map((seat) => seat.priceTier) ?? []),
                            ...(seatMap?.generalAdmissionSections.map((s) => s.priceTier) ?? []),
                          ]),
                        ]}
                      />
                    </>
                  ),
                },
                {
                  key: 'seatmap',
                  label: 'Seat map & gates',
                  children: (
                    <>
                      <EventSeatMapPanel
                        eventId={id}
                        seatMap={seatMap}
                        entryGates={entryGates}
                        isDraft={isDraft}
                        onChanged={reload}
                      />
                      {isDraft && (
                        <EntryGatesPanel
                          eventId={id}
                          gates={entryGates}
                          onGateCreated={refreshEntryGates}
                        />
                      )}
                    </>
                  ),
                },
                {
                  key: 'policies',
                  label: 'Policies',
                  children: <PolicyDocumentsPanel eventId={id} />,
                },
                {
                  key: 'sales',
                  label: 'Sales & access',
                  children: (
                    <>
                      <EventSlugCard event={event} onSaved={reload} />
                      {!isDraft && <SeatBlockPanel eventId={id} />}
                      {!isDraft && event.requiresQueue && <QueueSettingsPanel eventId={id} />}
                      {isDraft && (
                        <Typography.Text
                          type="secondary"
                          style={{ display: 'block', marginTop: 24 }}
                        >
                          Seat blocking and waiting-room settings appear here once the event is
                          published.
                        </Typography.Text>
                      )}
                    </>
                  ),
                },
              ]}
            />
          </Card>
        </PageContainer>
      </ScrollRegion>
    </>
  );
}
