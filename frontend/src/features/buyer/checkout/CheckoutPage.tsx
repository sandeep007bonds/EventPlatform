import { useEffect, useRef, useState } from 'react';
import { Button, Card, List, Result, Typography } from 'antd';
import type { AxiosError } from 'axios';
import { useNavigate, useParams } from 'react-router-dom';
import { getEvent } from '../../../services/catalog/catalogApi';
import { getHold, type HoldView } from '../../../services/inventory/inventoryApi';
import { checkout } from '../../../services/ordering/orderingApi';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { toast } from '../../../components/common/feedback/toast';
import { formatMoney } from '../../../utils/money';

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

  const [hold, setHold] = useState<HoldView | null>(null);
  const [currency, setCurrency] = useState('USD');
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
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

    setSubmitting(true);
    try {
      const result = await checkout(holdId, idempotencyKey.current);
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

  return (
    <Card title="Confirm your order">
      <Typography.Text strong>
        Time remaining:{' '}
        {expired
          ? 'expired'
          : `${Math.floor(secondsLeft / 60)}:${String(secondsLeft % 60).padStart(2, '0')}`}
      </Typography.Text>

      <List
        style={{ marginTop: 16 }}
        dataSource={hold.lines}
        renderItem={(line) => (
          <List.Item>
            <span>{line.priceTier}</span>
            <span>{formatMoney(line.priceMinor, currency)}</span>
          </List.Item>
        )}
      />

      <Typography.Title level={4} style={{ marginTop: 16 }}>
        Total: {formatMoney(hold.totalMinor, currency)}
      </Typography.Title>

      <Button
        type="primary"
        size="large"
        block
        disabled={expired}
        loading={submitting}
        onClick={() => void handleConfirm()}
      >
        Confirm purchase
      </Button>
    </Card>
  );
}
