import { useState } from 'react';
import { Alert, Button, Typography } from 'antd';
import { AddressElement, PaymentElement, useElements, useStripe } from '@stripe/react-stripe-js';

export interface CheckoutPaymentFormProps {
  /** The hold being purchased — used to build the redirect-return URL. */
  holdId: string;
  /** The order this payment is for — used to build the redirect-return URL. */
  orderId: string;
  /** Whether the hold has expired — disables submission and swaps the button label. */
  expired: boolean;
  /** Whether the buyer email field is currently valid — gates submission alongside `expired`. */
  emailValid: boolean;
  /** Called once the payment resolves in-page (no redirect needed) as succeeded/pending. */
  onResolved: () => void;
}

/**
 * The payment step of checkout — must be rendered inside `<Elements>` (mounted by `CheckoutPage`
 * once a client secret exists from the backend's payment-intent create call), since it's the only
 * place `useStripe`/`useElements` are called (no conditional-hook hazard: this component only ever
 * mounts once Stripe is configured and a client secret is known). `PaymentElement` surfaces every
 * payment method enabled for the account's region (cards, UPI, etc.); `confirmPayment` handles
 * authentication (3-D Secure challenge, UPI app-switch) natively — most methods resolve in-page
 * (`redirect: 'if_required'`), the rest redirect out and back via `CheckoutReturnPage`. Raw payment
 * details only ever reach Stripe's own iframe/API, never our backend (PCI SAQ-A).
 */
export function CheckoutPaymentForm({
  holdId,
  orderId,
  expired,
  emailValid,
  onResolved,
}: CheckoutPaymentFormProps) {
  const stripe = useStripe();
  const elements = useElements();
  const [submitting, setSubmitting] = useState(false);
  const [stripeError, setStripeError] = useState<string | null>(null);

  const handleClick = async () => {
    if (!stripe || !elements) {
      return;
    }

    setSubmitting(true);
    setStripeError(null);
    try {
      const returnUrl = new URL(`/checkout/${holdId}/return`, window.location.origin);
      returnUrl.searchParams.set('orderId', orderId);

      const { error, paymentIntent } = await stripe.confirmPayment({
        elements,
        confirmParams: { return_url: returnUrl.toString() },
        redirect: 'if_required',
      });

      if (error) {
        setStripeError(error.message ?? 'Could not process that payment. Please try again.');
        return;
      }

      if (paymentIntent?.status === 'succeeded' || paymentIntent?.status === 'processing') {
        onResolved();
      }
      // Any other in-page outcome (e.g. requires_payment_method after a decline) stays on this
      // page — Payment Element already shows its own inline error for that case.
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div>
      <PaymentElement />

      {/*
        Billing name + address. Required by Stripe for export transactions from an India-registered
        account (RBI rules) — without it the payment is rejected outright. Because this sits inside
        the same <Elements> group, confirmPayment({ elements }) picks it up automatically as the
        payment method's billing details; it never passes through our own backend.
      */}
      <Typography.Text strong style={{ display: 'block', marginTop: 20, marginBottom: 8 }}>
        Billing details
      </Typography.Text>
      <AddressElement options={{ mode: 'billing' }} />

      {stripeError && (
        <Alert
          type="error"
          showIcon
          message={stripeError}
          style={{ marginTop: 12, marginBottom: 12 }}
        />
      )}
      <Button
        type="primary"
        size="large"
        block
        disabled={expired || !emailValid}
        loading={submitting}
        onClick={() => void handleClick()}
        style={{ marginTop: 20 }}
      >
        {expired ? 'Hold expired' : 'Confirm purchase'}
      </Button>
    </div>
  );
}
