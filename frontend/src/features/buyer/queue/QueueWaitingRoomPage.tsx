import { useEffect, useState } from 'react';
import { Card, Result, Spin, Typography } from 'antd';
import { useNavigate, useParams } from 'react-router-dom';
import { getQueueStatus, joinQueue } from '../../../services/queue/queueApi';
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
 */
export function QueueWaitingRoomPage() {
  const { id: eventId } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [position, setPosition] = useState<number | null>(null);
  const [estimatedWaitSeconds, setEstimatedWaitSeconds] = useState<number | null>(null);
  const [error, setError] = useState(false);

  useEffect(() => {
    if (!eventId) {
      return;
    }

    let cancelled = false;
    const sessionId = getOrCreateQueueSessionId(eventId);

    const onAdmitted = (admissionToken: string) => {
      storeAdmissionToken(eventId, admissionToken);
      void navigate(`/events/${eventId}/seats`, { replace: true });
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

    return () => {
      cancelled = true;
    };
  }, [eventId, navigate]);

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
