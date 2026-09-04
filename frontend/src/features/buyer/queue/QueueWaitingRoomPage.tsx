import { useEffect, useState } from 'react';
import { Card, Result, Spin, Typography } from 'antd';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { getEvent, getEventBySlug, type EventResponse } from '../../../services/catalog/catalogApi';
import { getQueueStatus, joinQueue } from '../../../services/queue/queueApi';
import { upcomingSellableSessions } from '../../../utils/eventSessions';
import { PageHeader } from '../../../components/common/layout/PageHeader';
import { toast } from '../../../components/common/feedback/toast';
import { getOrCreateQueueSessionId, storeAdmissionToken } from '../../../utils/queueAdmission';

const QUEUE_POLL_INTERVAL_MS = 3000;

/**
 * The virtual waiting room for an event that has `requiresQueue` set. Public, anonymous — a buyer
 * joins with a client-generated session id (stashed in sessionStorage so a page refresh resumes
 * the same position rather than re-enqueueing at the back), then polls until admitted, at which
 * point the admission token is stashed and the buyer is sent on to seat selection automatically.
 * Unlike ticket-issuance polling elsewhere in this app, this has no max-attempts cap — a queue can
 * legitimately take a long time, so it keeps polling for as long as the tab stays open.
 *
 * **The room is keyed on the event, not a performance** (ADR-0039), and that is deliberate: one
 * waiting room gates one on-sale, and an on-sale puts the whole run on sale at once. Queueing per
 * night would make a buyer queue three times for a three-night run. Which night they are heading
 * for rides along in `?eventSessionId=` purely so admission can hand them straight to the right
 * seat map; it plays no part in the queue itself.
 *
 * Note the two different "session" words here: `queueSessionId` is this buyer's place in line,
 * while `eventSessionId` is a performance. They are unrelated, which is exactly why every route,
 * parameter and field in this codebase spells the second one out in full.
 */
export function QueueWaitingRoomPage() {
  const { eventSlug } = useParams<{ eventSlug: string }>();
  const [searchParams] = useSearchParams();
  const requestedSessionId = searchParams.get('eventSessionId');
  const navigate = useNavigate();

  const [position, setPosition] = useState<number | null>(null);
  const [estimatedWaitSeconds, setEstimatedWaitSeconds] = useState<number | null>(null);
  const [error, setError] = useState(false);

  useEffect(() => {
    if (!eventSlug) {
      return;
    }

    let cancelled = false;

    // The route param may be a slug, but the Queue service and the admission-token store are both
    // keyed on the event's id — so resolve it once before joining anything.
    (isGuid(eventSlug) ? getEvent(eventSlug) : getEventBySlug(eventSlug))
      .then((event) => {
        if (!cancelled) {
          run(event);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setError(true);
        }
      });

    function run(event: EventResponse) {
      const eventId = event.id;
      const sessionId = getOrCreateQueueSessionId(eventId);

      const onAdmitted = (admissionToken: string) => {
        storeAdmissionToken(eventId, admissionToken);

        // Back to the night they came in for. If they arrived without one — a bookmarked waiting
        // room, say — the next performance still on sale is the only sensible destination.
        const target =
          upcomingSellableSessions(event).find((session) => session.id === requestedSessionId) ??
          upcomingSellableSessions(event)[0];

        void navigate(
          target ? `/events/${event.slug}/s/${target.id}/seats` : `/events/${event.slug}`,
          { replace: true },
        );
      };

      const poll = () => {
        getQueueStatus(eventId, sessionId)
          .then((status) => {
            if (cancelled) {
              return;
            }
            if (status.admitted && status.admissionToken) {
              onAdmitted(status.admissionToken);
              return;
            }
            setPosition(status.position);
            setEstimatedWaitSeconds(status.estimatedWaitSeconds);
            setTimeout(poll, QUEUE_POLL_INTERVAL_MS);
          })
          .catch(() => {
            if (!cancelled) {
              setError(true);
            }
          });
      };

      joinQueue(eventId, sessionId)
        .then((status) => {
          if (cancelled) {
            return;
          }
          if (status.admitted && status.admissionToken) {
            onAdmitted(status.admissionToken);
            return;
          }
          setPosition(status.position);
          setEstimatedWaitSeconds(status.estimatedWaitSeconds);
          setTimeout(poll, QUEUE_POLL_INTERVAL_MS);
        })
        .catch(() => {
          if (!cancelled) {
            setError(true);
          }
          toast.error('Could not join the queue — please try again.');
        });
    }

    return () => {
      cancelled = true;
    };
  }, [eventSlug, requestedSessionId, navigate]);

  if (error) {
    return (
      <Result
        status="error"
        title="Something went wrong"
        subTitle="We couldn't reach the waiting room. Please refresh to try again."
      />
    );
  }

  return (
    <div style={{ maxWidth: 480, margin: '0 auto' }}>
      <PageHeader
        title="You're in line"
        description="Hang tight — we'll take you to seat selection the moment it's your turn."
      />
      <Card style={{ textAlign: 'center', padding: '32px 16px' }}>
        <Spin size="large" />
        {position != null && (
          <Typography.Title level={3} style={{ marginTop: 24 }}>
            Position {position + 1}
          </Typography.Title>
        )}
        {estimatedWaitSeconds != null && (
          <Typography.Text type="secondary">
            Estimated wait: about {Math.max(1, Math.ceil(estimatedWaitSeconds / 60))} minute
            {estimatedWaitSeconds > 60 ? 's' : ''}
          </Typography.Text>
        )}
        <Typography.Paragraph type="secondary" style={{ marginTop: 16 }}>
          Please keep this page open — you'll be moved on automatically.
        </Typography.Paragraph>
      </Card>
    </div>
  );
}

/** Whether the route param is an event id rather than a slug — see `EventDetailPage`. */
function isGuid(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);
}
