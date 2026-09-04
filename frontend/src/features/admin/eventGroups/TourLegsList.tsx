import { useEffect, useState } from 'react';
import { Button, Empty, Space, Tag, Typography } from 'antd';
import dayjs from 'dayjs';
import { Link } from 'react-router-dom';
import {
  getEventGroup,
  listEvents,
  type EventGroupResponse,
  type EventResponse,
} from '../../../services/catalog/catalogApi';
import { eventStatusColor } from '../../../utils/eventStatus';
import { LoadError } from '../../../components/common/errors/LoadError';

interface TourLegsListProps {
  eventGroupId: string;
  /** Hides one leg from the list — the event currently being viewed/edited, if any. */
  excludeEventId?: string;
  /** Shows the tour's own title as a heading above the legs. Off when the caller already
   * displays it elsewhere (e.g. right next to the tour picker that produced this id). */
  showTitle?: boolean;
}

/**
 * Compact, read-only list of a tour's other legs — upcoming ones always visible, past ones
 * collapsed behind a toggle so a long-running tour doesn't dump its whole history inline. Gives
 * an organizer context (dates already claimed, how many legs exist) right where they're about to
 * add another leg or are looking at one. Every leg's own page-detail lives at `/admin/events/{id}`.
 */
export function TourLegsList({
  eventGroupId,
  excludeEventId,
  showTitle = true,
}: TourLegsListProps) {
  const [tour, setTour] = useState<EventGroupResponse | null>(null);
  const [legs, setLegs] = useState<EventResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [showPast, setShowPast] = useState(false);
  const [reloadToken, setReloadToken] = useState(0);

  useEffect(() => {
    let cancelled = false;

    Promise.all([
      showTitle ? getEventGroup(eventGroupId) : Promise.resolve(null),
      listEvents({ eventGroupId, mine: true, pageSize: 100 }),
    ])
      .then(([group, result]) => {
        if (cancelled) {
          return;
        }
        setTour(group);
        setLegs(result.events.filter((leg) => leg.id !== excludeEventId));
        setLoadError(false);
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
  }, [eventGroupId, excludeEventId, showTitle, reloadToken]);

  if (loading) {
    return (
      <Typography.Text type="secondary" style={{ display: 'block' }}>
        Loading tour legs…
      </Typography.Text>
    );
  }

  if (loadError) {
    return (
      <LoadError
        description="Could not load this tour's legs."
        onRetry={() => {
          setLoading(true);
          setReloadToken((token) => token + 1);
        }}
      />
    );
  }

  // A leg's dates now live on its performances, and the server keeps the run's span denormalised
  // on the event — so a leg is "upcoming" until its *last* performance has finished, and sorts by
  // its first. A leg with no performances yet has neither, and sorts last among the upcoming.
  const now = dayjs();
  const runStart = (leg: EventResponse) =>
    leg.firstSessionStartsAt == null
      ? Number.MAX_SAFE_INTEGER
      : dayjs(leg.firstSessionStartsAt).valueOf();
  const hasFinished = (leg: EventResponse) =>
    leg.lastSessionEndsAt != null && !dayjs(leg.lastSessionEndsAt).isAfter(now);

  const upcoming = legs
    .filter((leg) => !hasFinished(leg))
    .sort((a, b) => runStart(a) - runStart(b));
  const past = legs.filter(hasFinished).sort((a, b) => runStart(b) - runStart(a));

  const renderLeg = (leg: EventResponse) => (
    <Space key={leg.id} style={{ width: '100%', justifyContent: 'space-between' }}>
      <Link to={`/admin/events/${leg.id}`}>{leg.title}</Link>
      <Space size={8}>
        <Typography.Text type="secondary">
          {leg.firstSessionStartsAt == null
            ? 'No performances yet'
            : dayjs(leg.firstSessionStartsAt).format('MMM D, YYYY')}
        </Typography.Text>
        <Tag color={eventStatusColor[leg.status]}>{leg.status}</Tag>
      </Space>
    </Space>
  );

  return (
    <div>
      {showTitle && tour && (
        <Typography.Text strong style={{ display: 'block', marginBottom: 8 }}>
          {tour.title}
        </Typography.Text>
      )}

      {upcoming.length === 0 && past.length === 0 && (
        <Empty
          description="No other legs yet"
          image={Empty.PRESENTED_IMAGE_SIMPLE}
          style={{ margin: '8px 0' }}
        />
      )}

      {upcoming.length > 0 && (
        <Space direction="vertical" size={6} style={{ width: '100%' }}>
          {upcoming.map(renderLeg)}
        </Space>
      )}

      {past.length > 0 && (
        <div style={{ marginTop: upcoming.length > 0 ? 12 : 0 }}>
          <Button
            type="link"
            style={{ paddingLeft: 0 }}
            onClick={() => setShowPast((prev) => !prev)}
          >
            {showPast ? 'Hide' : 'Show'} {past.length} past leg{past.length === 1 ? '' : 's'}
          </Button>
          {showPast && (
            <Space direction="vertical" size={6} style={{ width: '100%' }}>
              {past.map(renderLeg)}
            </Space>
          )}
        </div>
      )}
    </div>
  );
}
