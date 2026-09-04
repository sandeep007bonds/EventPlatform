import { useEffect, useState } from 'react';
import { Button, Card, Descriptions, Space, Tabs, Tag, theme, Typography } from 'antd';
import type { AxiosError } from 'axios';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import {
  getEvent,
  listTicketTypes,
  pauseSales,
  publishEvent,
  resumeSales,
  type EventResponse,
  type TicketTypeResponse,
} from '../../../services/catalog/catalogApi';
import { primarySession, runLabel, venueLabel } from '../../../utils/eventSessions';
import { formatEventDateTime } from '../../../utils/eventTime';
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
import { QueueSettingsPanel } from './QueueSettingsPanel';
import { PromoCodesPanel } from '../promoCodes/PromoCodesPanel';
import { TicketTypesPanel } from '../ticketTypes/TicketTypesPanel';
import { PolicyDocumentsPanel } from '../policies/PolicyDocumentsPanel';
import { EventPresentationForm } from './EventPresentationForm';
import { EventSellingRulesForm } from './EventSellingRulesForm';
import { EventPerformancesPanel } from './EventPerformancesPanel';
import { EventSlugCard } from './EventSlugCard';

const TAB_QUERY_PARAM = 'tab';

/**
 * Organizer's event workspace.
 *
 * Grouped into tabs rather than one long scroll, because the sections have genuinely different
 * lifecycles: the event page stays editable forever, the selling rules lock at publish, the
 * performances are where the venue and the dates live, and seat blocking only exists afterwards.
 * One page made that look like one decision. **Each tab saves on its own** — a tab is a unit of
 * work, and a single Save spanning "the title" and "the tax rate" would have to fail as a whole
 * when only one half is still allowed.
 *
 * The active tab lives in the query string so a reload, a bookmark or a link to "the policies of
 * this event" all land where they should.
 */
export function AdminEventDetailPage() {
  const { eventId: id } = useParams<{ eventId: string }>();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const { token } = theme.useToken();

  const [event, setEvent] = useState<EventResponse | null>(null);
  const [ticketTypes, setTicketTypes] = useState<TicketTypeResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [loadError, setLoadError] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [togglingSales, setTogglingSales] = useState(false);

  const load = (eventId: string) => {
    Promise.all([getEvent(eventId), listTicketTypes(eventId).catch(() => [])])
      .then(([eventResult, ticketTypesResult]) => {
        setEvent(eventResult);
        setTicketTypes(ticketTypesResult);
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
      // Publishing is all-or-nothing across performances, and the server answers with every
      // problem it found rather than the first — a run with three unallocated blocks is three
      // things to fix, not three round trips.
      toast.error(
        'Could not publish — every performance needs a published seat map with all of its blocks allocated.',
      );
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
      if (event.allSalesPaused) {
        await resumeSales(id);
        toast.success('Sales resumed across every performance.');
      } else {
        await pauseSales(id);
        toast.success('Sales paused across every performance.');
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
  const featured = primarySession(event);
  // What the Publish button is waiting on. Catalog refuses a publish unless every performance names
  // a published seat-map version and allocates every one of its blocks, so saying so here is more
  // useful than a button that only explains itself after being clicked.
  const unreadySessions = event.sessions.filter(
    (session) => session.seatMapVersionId == null || session.allocations.length === 0,
  );

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
                {event.allSalesPaused && <Tag color="warning">Sales paused</Tag>}
              </Space>
            }
            extra={
              <>
                {isDraft && event.sessions.length > 0 && unreadySessions.length === 0 && (
                  <Button type="primary" loading={submitting} onClick={() => void handlePublish()}>
                    Publish
                  </Button>
                )}
                {event.status === 'Published' && (
                  <Button
                    danger={!event.allSalesPaused}
                    loading={togglingSales}
                    onClick={() => void handleToggleSales()}
                  >
                    {event.allSalesPaused ? 'Resume sales' : 'Pause all sales'}
                  </Button>
                )}
                <Button onClick={() => void navigate('/admin')}>← Back to events</Button>
              </>
            }
          />

          <Card style={{ marginBottom: 24 }}>
            <Descriptions column={{ xs: 1, sm: 2, md: 4 }} size="small">
              <Descriptions.Item label="Runs">
                {runLabel(event) ?? 'No performances yet'}
              </Descriptions.Item>
              <Descriptions.Item label="Performances">{event.sessions.length}</Descriptions.Item>
              <Descriptions.Item label="Venue">
                {venueLabel(featured) ?? 'Not set'}
              </Descriptions.Item>
              <Descriptions.Item label="Time zone">
                {featured?.timeZoneId ?? 'Not set'}
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
              <Descriptions.Item label="On sale">
                {event.onSaleAt
                  ? formatEventDateTime(event.onSaleAt, featured?.timeZoneId)
                  : 'Immediately'}
              </Descriptions.Item>
            </Descriptions>
            {isDraft && event.sessions.length === 0 && (
              <Typography.Text type="warning" style={{ display: 'block', marginTop: 12 }}>
                Add a performance on the Performances tab before you can publish.
              </Typography.Text>
            )}
            {isDraft && unreadySessions.length > 0 && (
              <Typography.Text type="warning" style={{ display: 'block', marginTop: 12 }}>
                {unreadySessions.length} of {event.sessions.length} performance
                {event.sessions.length === 1 ? '' : 's'} still needs a published seat map with every
                block allocated to a ticket type.
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
              // the header happens to be today. The background must be a real colour: `inherit`
              // resolves transparent here, and the form scrolls through the pinned row.
              renderTabBar={(tabBarProps, DefaultTabBar) => (
                <div
                  style={{
                    position: 'sticky',
                    top: 0,
                    zIndex: 3,
                    margin: '-28px -28px 0',
                    padding: '28px 28px 0',
                    background: token.colorBgContainer,
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
                  key: 'performances',
                  label: (
                    <Space size={6}>
                      Performances
                      {event.sessions.length > 0 && <Tag>{event.sessions.length}</Tag>}
                    </Space>
                  ),
                  children: <EventPerformancesPanel event={event} />,
                },
                {
                  key: 'rules',
                  label: (
                    <Space size={6}>
                      Selling rules
                      {!isDraft && <Tag>Locked</Tag>}
                    </Space>
                  ),
                  children: <EventSellingRulesForm event={event} onSaved={reload} />,
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
                        ticketTypes={ticketTypes}
                      />
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
                      {!isDraft && <SeatBlockPanel event={event} />}
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
