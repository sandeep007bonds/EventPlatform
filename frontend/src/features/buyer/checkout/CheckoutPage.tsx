import { useEffect, useRef, useState } from 'react';
import { Button, Card, Input, Progress, Result, Tag, Typography } from 'antd';
import { ClockCircleOutlined, MailOutlined } from '@ant-design/icons';
import type { AxiosError } from 'axios';
import { useNavigate, useParams } from 'react-router-dom';
import { getEvent } from '../../../services/catalog/catalogApi';
import { getHold, type HoldView } from '../../../services/inventory/inventoryApi';
import { checkout } from '../../../services/ordering/orderingApi';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { toast } from '../../../components/common/feedback/toast';
import { formatMoney } from '../../../utils/money';
import { useAuth } from '../../../contexts/useAuth';

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

const HOLD_TTL_SECONDS_ESTIMATE = 120;

interface CheckoutErrorBody {
  message?: string;
}

function useCountdown(expiresAt: string | null): number {
  const [secondsLeft, setSecondsLeft] = useState(0);

  useEffect(() => {
    if (!expiresAt) {
      return;
    }

    const tick = () =>
      setSecondsLeft(Math.max(0, Math.floor((new Date(expiresAt).getTime() - Date.now()) / 1000)));
    tick();
    const interval = setInterval(tick, 1000);
    return () => clearInterval(interval);
  }, [expiresAt]);

  return secondsLeft;
}

/** Hold summary + confirm purchase, with a live countdown against the hold's expiry. */
export function CheckoutPage() {
  const { holdId } = useParams<{ holdId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();

  const [hold, setHold] = useState<HoldView | null>(null);
  const [currency, setCurrency] = useState('USD');
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [buyerEmail, setBuyerEmail] = useState(user?.email ?? '');
  // Generated once per mount and reused across retries of this same attempt — never regenerated
  // on retry, so the backend's idempotency check actually dedupes.
  const idempotencyKey = useRef(crypto.randomUUID());

  useEffect(() => {
    if (!holdId) {
      return;
    }

    let cancelled = false;
    getHold(holdId)
      .then(async (result) => {
        if (cancelled) {
          return;
        }
        setHold(result);
        const event = await getEvent(result.catalogEventId).catch(() => null);
        if (!cancelled && event) {
          setCurrency(event.currency);
        }
      })
      .catch(() => toast.error('Could not load this hold — it may have expired.'))
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [holdId]);

  const secondsLeft = useCountdown(hold?.expiresAt ?? null);

  const handleConfirm = async () => {
    if (!holdId) {
      return;
    }

    if (!EMAIL_PATTERN.test(buyerEmail)) {
      toast.error('Enter a valid email so we can send your tickets.');
      return;
    }

    setSubmitting(true);
    try {
      const result = await checkout(holdId, idempotencyKey.current, buyerEmail);
      void navigate(`/orders/${result.orderId}`);
    } catch (error) {
      const axiosError = error as AxiosError<CheckoutErrorBody>;
      const status = axiosError.response?.status;
      const message = axiosError.response?.data?.message;

      if (status === 404) {
        toast.error('This hold no longer exists.');
      } else if (status === 409 || status === 422) {
        toast.error(message ?? 'This purchase could not be completed.');
      } else if (status === 403) {
        toast.error('This hold does not belong to you.');
      } else {
        toast.error('Something went wrong. Please try again.');
      }
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return <DetailSkeleton />;
  }

  if (!hold) {
    return (
      <Result
        status="error"
        title="This hold is no longer available"
        subTitle="It may have expired. Go back and pick your seats again."
      />
    );
  }

  const expired = secondsLeft <= 0;
  const nearlyExpired = secondsLeft > 0 && secondsLeft <= 30;
  const countdownLabel = expired
    ? 'Expired'
    : `${Math.floor(secondsLeft / 60)}:${String(secondsLeft % 60).padStart(2, '0')}`;

  return (
    <div style={{ maxWidth: 560, margin: '0 auto' }}>
      <Card
        title="Confirm your order"
        styles={{ body: { padding: 24 } }}
        extra={
          <Tag
            icon={<ClockCircleOutlined />}
            color={expired ? 'error' : nearlyExpired ? 'warning' : 'processing'}
          >
            {countdownLabel}
          </Tag>
        }
      >
        <Progress
          percent={Math.min(100, (secondsLeft / HOLD_TTL_SECONDS_ESTIMATE) * 100)}
          showInfo={false}
          size="small"
          status={expired ? 'exception' : nearlyExpired ? 'active' : 'normal'}
          style={{ marginBottom: 20 }}
        />

        <div>
          {hold.lines.map((line, index) => (
            <div
              key={`${line.inventoryItemId ?? line.generalAdmissionAllocationId}-${index}`}
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                padding: '10px 0',
                borderBottom: '1px solid rgba(0,0,0,0.06)',
              }}
            >
              <Typography.Text>
                {line.priceTier}
                {line.generalAdmissionAllocationId ? ` × ${line.quantity}` : ''}
              </Typography.Text>
              <Typography.Text>{formatMoney(line.priceMinor, currency)}</Typography.Text>
            </div>
          ))}
        </div>

        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'baseline',
            marginTop: 16,
            marginBottom: 20,
          }}
        >
          <Typography.Text strong style={{ fontSize: 16 }}>
            Total
          </Typography.Text>
          <Typography.Title level={3} style={{ margin: 0 }}>
            {formatMoney(hold.totalMinor, currency)}
          </Typography.Title>
        </div>

        <Typography.Text strong style={{ display: 'block', marginBottom: 6 }}>
          Email for ticket delivery
        </Typography.Text>
        <Input
          prefix={<MailOutlined />}
          type="email"
          placeholder="you@example.com"
          value={buyerEmail}
          onChange={(event) => setBuyerEmail(event.target.value)}
          style={{ marginBottom: 20 }}
        />

        <Button
          type="primary"
          size="large"
          block
          disabled={expired}
          loading={submitting}
          onClick={() => void handleConfirm()}
        >
          {expired ? 'Hold expired' : 'Confirm purchase'}
        </Button>
      </Card>
    </div>
  );
}
