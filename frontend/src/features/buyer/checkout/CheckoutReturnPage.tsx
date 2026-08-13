import { useEffect, useState } from 'react';
import { Button, Result } from 'antd';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { stripePromise } from '../../../services/payments/stripeClient';
import { syncOrderPayment } from '../../../services/ordering/orderingApi';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';

type ReturnStatus = 'checking' | 'succeeded' | 'failed';

/**
 * Landing page for payment methods that redirect out of the app to authenticate (a 3-D Secure
 * challenge frame, a UPI app-switch) and back — `CheckoutPaymentForm`'s `confirmPayment` call sets
 * this as its `return_url`. Stripe appends `payment_intent_client_secret` itself; `orderId` is our
 * own addition. Retrieves the intent's final status directly from Stripe (no `<Elements>` context
 * here, so `stripePromise` is used directly rather than the `useStripe`/`useElements` hooks), nudges
 * the backend to reconcile immediately, and routes accordingly — exactly as an in-page resolution
 * does. The webhook and the saga's own poll remain the backstops if this page is never reached.
 */
export function CheckoutReturnPage() {
  const { holdId } = useParams<{ holdId: string }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const orderId = searchParams.get('orderId');
  const clientSecret = searchParams.get('payment_intent_client_secret');

  // No clientSecret means this page was reached without a real Stripe redirect (e.g. a stale
  // bookmark) — that's a synchronous, derivable-from-props initial state, not something to set from
  // inside the effect below.
  const [status, setStatus] = useState<ReturnStatus>(() => (clientSecret ? 'checking' : 'failed'));

  useEffect(() => {
    if (!clientSecret) {
      return;
    }

    let cancelled = false;
    stripePromise
      ?.then((stripe) => stripe?.retrievePaymentIntent(clientSecret))
      .then((result) => {
        if (cancelled) {
          return;
        }
        const paymentStatus = result?.paymentIntent?.status;
        const succeeded = paymentStatus === 'succeeded' || paymentStatus === 'processing';

        // Best-effort nudge so the order confirms now rather than on the saga's next poll; the
        // navigation below must not depend on it, hence the swallowed rejection.
        if (succeeded && orderId) {
          void syncOrderPayment(orderId).catch(() => {});
        }

        setStatus(succeeded ? 'succeeded' : 'failed');
      })
      .catch(() => {
        if (!cancelled) {
          setStatus('failed');
        }
      });

    return () => {
      cancelled = true;
    };
  }, [clientSecret, orderId]);

  useEffect(() => {
    if (status === 'succeeded' && orderId) {
      void navigate(`/orders/${orderId}`, { replace: true });
    }
  }, [status, orderId, navigate]);

  if (status !== 'failed') {
    // Either still checking, or succeeded and about to navigate away — either way this is a brief
    // interim frame.
    return <DetailSkeleton />;
  }

  return (
    <Result
      status="error"
      title="That payment didn't go through"
      subTitle="Your seats are still held — you can try again with a different payment method."
      extra={
        holdId ? (
          <Button type="primary" onClick={() => void navigate(`/checkout/${holdId}`)}>
            Try again
          </Button>
        ) : undefined
      }
    />
  );
}
