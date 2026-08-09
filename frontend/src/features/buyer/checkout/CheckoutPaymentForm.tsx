import { useState } from 'react';
import { Alert, Button, Typography } from 'antd';
import { CardElement, useElements, useStripe } from '@stripe/react-stripe-js';
import type { StripeCardElementChangeEvent } from '@stripe/stripe-js';

export interface CheckoutPaymentFormProps {
  /** Whether the hold has expired — disables submission and swaps the button label. */
  expired: boolean;
  /** Whether the buyer email field is currently valid — gates submission alongside `expired`. */
  emailValid: boolean;
  /** True while the parent's own backend checkout call is in flight. */
  submitting: boolean;
  /** Called once client-side tokenization succeeds, with the opaque Stripe `pm_...` id. */
  onSubmit: (paymentMethodId: string) => void;
}

const CARD_ELEMENT_OPTIONS = {
  style: {
    base: {
      fontSize: '16px',
      color: '#1f1f1f',
      '::placeholder': { color: '#999999' },
    },
    invalid: { color: '#ff4d4f' },
  },
};

/**
 * The card-entry + submit step of checkout — must be rendered inside `<Elements>`, since it's the
 * only place `useStripe`/`useElements` are called (no conditional-hook hazard: this component only
 * ever mounts when Stripe is configured). Raw card data only ever enters Stripe's own iframe
 * (`CardElement`); `createPaymentMethod` tokenizes it directly against Stripe's API from the
 * browser, and only the resulting opaque id is ever handed to the parent (PCI SAQ-A) — a
 * client-side tokenization failure is shown inline here and never reaches the backend at all.
 */
export function CheckoutPaymentForm({
  expired,
  emailValid,
  submitting,
  onSubmit,
}: CheckoutPaymentFormProps) {
  const stripe = useStripe();
  const elements = useElements();
  const [cardComplete, setCardComplete] = useState(false);
  const [stripeError, setStripeError] = useState<string | null>(null);
  const [tokenizing, setTokenizing] = useState(false);

  const handleCardChange = (event: StripeCardElementChangeEvent) => {
    setCardComplete(event.complete);
    setStripeError(event.error?.message ?? null);
  };

  const handleClick = async () => {
    const cardElement = elements?.getElement(CardElement);
    if (!stripe || !cardElement) {
      return;
    }

    setTokenizing(true);
    setStripeError(null);
    try {
      const { paymentMethod, error } = await stripe.createPaymentMethod({
        type: 'card',
        card: cardElement,
      });

      if (error || !paymentMethod) {
        setStripeError(error?.message ?? 'Could not process that card. Please try again.');
        return;
      }

      onSubmit(paymentMethod.id);
    } finally {
      setTokenizing(false);
    }
  };

  return (
    <div>
      <Typography.Text strong style={{ display: 'block', marginBottom: 6 }}>
        Card details
      </Typography.Text>
      <div
        style={{
          border: '1px solid #d9d9d9',
          borderRadius: 8,
          padding: '11px 12px',
          marginBottom: stripeError ? 8 : 20,
        }}
      >
        <CardElement options={CARD_ELEMENT_OPTIONS} onChange={handleCardChange} />
      </div>
      {stripeError && (
        <Alert type="error" showIcon message={stripeError} style={{ marginBottom: 20 }} />
      )}
      <Button
        type="primary"
        size="large"
        block
        disabled={expired || !emailValid || !cardComplete}
        loading={tokenizing || submitting}
        onClick={() => void handleClick()}
      >
        {expired ? 'Hold expired' : 'Confirm purchase'}
      </Button>
    </div>
  );
}
