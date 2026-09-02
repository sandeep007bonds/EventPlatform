import { useEffect, useRef, useState } from 'react';
import { Alert, Button, Card, Input, Progress, Result, Select, Space, Tag, Typography } from 'antd';
import { ClockCircleOutlined, MailOutlined, TagOutlined } from '@ant-design/icons';
import { Elements } from '@stripe/react-stripe-js';
import type { AxiosError } from 'axios';
import { useNavigate, useParams } from 'react-router-dom';
import {
  getEvent,
  listPublicPromoCodes,
  type PublicPromoCodeResponse,
} from '../../../services/catalog/catalogApi';
import { getHold, type HoldView } from '../../../services/inventory/inventoryApi';
import {
  checkout,
  quoteCheckout,
  type CheckoutQuoteResponse,
} from '../../../services/ordering/orderingApi';
import { isStripeConfigured, stripePromise } from '../../../services/payments/stripeClient';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { toast } from '../../../components/common/feedback/toast';
import { formatMoney } from '../../../utils/money';
import { useAuth } from '../../../contexts/useAuth';
import { CheckoutPaymentForm } from './CheckoutPaymentForm';
import { PriceRow } from './PriceRow';

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

const HOLD_TTL_SECONDS_ESTIMATE = 120;

/**
 * Why a code wasn't accepted, in the buyer's language. The backend returns a machine-readable
 * reason rather than prose precisely so this text lives here and can be translated.
 */
const PROMO_REJECTION_MESSAGES: Record<string, string> = {
  NotFound: "We don't recognise that code for this event.",
  Inactive: 'That code is no longer active.',
  NotYetValid: "That code isn't valid yet.",
  Expired: 'That code has expired.',
  RedemptionLimitReached: 'That code has been fully claimed.',
  BuyerLimitReached: "You've already used that code as many times as it allows.",
  NotApplicableToSelection: "That code doesn't apply to the tickets you've picked.",
};

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
  // Set once the backend has created (but not confirmed) a payment intent — the point at which the
  // page swaps from the plain "Continue" button to Payment Element. `null` again means either
  // nothing submitted yet, or the payment already resolved synchronously (dev fallback) and we
  // navigated away.
  const [intent, setIntent] = useState<{ orderId: string; clientSecret: string } | null>(null);
  // The server's price breakdown. Null only if the quote call itself failed, in which case the
  // summary falls back to the hold's own (undiscounted, untaxed) total.
  const [quote, setQuote] = useState<CheckoutQuoteResponse | null>(null);
  const [publicCodes, setPublicCodes] = useState<PublicPromoCodeResponse[]>([]);
  const [promoInput, setPromoInput] = useState('');
  const [applyingPromo, setApplyingPromo] = useState(false);
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
        // All three are independent and none is fatal on its own: without the event we keep the
        // default currency, without a quote we fall back to the hold's total, without public codes
        // the buyer can still type one in.
        const [event, initialQuote, codes] = await Promise.all([
          getEvent(result.catalogEventId).catch(() => null),
          quoteCheckout(holdId).catch(() => null),
          listPublicPromoCodes(result.catalogEventId).catch(() => []),
        ]);
        if (cancelled) {
          return;
        }
        if (event) {
          setCurrency(event.currency);
        }
        setQuote(initialQuote);
        setPublicCodes(codes);
      })
      .catch(() => {
        // hold stays null — the render below already shows a proper "no longer available" Result.
      })
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

  // Only ever what the *server* accepted, never what the buyer typed — so the code sent to
  // checkout is the same one that produced the total shown above the button.
  const appliedCode = quote?.promoCodeApplied ?? null;

  // Re-prices the hold with (or, for an empty code, without) a discount code. A rejected code is a
  // successful call with a reason attached, not an error: the quote is still the real price.
  const handleApplyPromo = async (code: string) => {
    if (!holdId) {
      return;
    }

    const trimmed = code.trim();
    setApplyingPromo(true);
    try {
      const result = await quoteCheckout(holdId, trimmed || null);
      setQuote(result);
      if (result.promoCodeApplied) {
        toast.success(`Code ${result.promoCodeApplied} applied.`);
      } else if (trimmed && result.promoCodeRejection) {
        toast.error(
          PROMO_REJECTION_MESSAGES[result.promoCodeRejection] ?? "That code couldn't be applied.",
        );
      }
    } catch {
      toast.error("Couldn't check that code. Please try again.");
    } finally {
      setApplyingPromo(false);
    }
  };

  const handleRemovePromo = () => {
    setPromoInput('');
    void handleApplyPromo('');
  };

  // Starts checkout: the backend creates (but does not confirm) a payment intent before the buyer
  // ever sees a payment form — a deliberate flow inversion from the old card-only design, where
  // tokenization happened first. A `null` clientSecret means the payment already resolved
  // synchronously (the no-Stripe-configured dev fallback), so we navigate straight to the order.
  const handleStartCheckout = async () => {
    if (!holdId) {
      return;
    }

    if (!EMAIL_PATTERN.test(buyerEmail)) {
      toast.error('Enter a valid email so we can send your tickets.');
      return;
    }

    if (buyerEmail.length > 320) {
      toast.error('That email is too long (max 320 characters).');
      return;
    }

    setSubmitting(true);
    try {
      const result = await checkout(holdId, idempotencyKey.current, buyerEmail, appliedCode);
      // Loose == null on purpose: it catches both null and undefined. The API omits null
      // fields entirely (JsonIgnoreCondition.WhenWritingNull), so a strict === null let the
      // no-Stripe path fall through and mount an empty payment form.
      if (result.clientSecret == null) {
        void navigate(`/orders/${result.orderId}`);
        return;
      }

      setIntent({ orderId: result.orderId, clientSecret: result.clientSecret });
    } catch (error) {
      const axiosError = error as AxiosError<CheckoutErrorBody>;
      const status = axiosError.response?.status;
      const message = axiosError.response?.data?.message;

      if (status === 404) {
        toast.error('This hold no longer exists.');
      } else if (status === 409 || status === 422) {
        toast.error(message ?? 'This purchase could not be completed.');
        // A code can lapse or run out between the quote and the charge — the saga re-checks and
        // refuses rather than quietly charging full price, so re-price to show what changed.
        if (appliedCode) {
          const refreshed = await quoteCheckout(holdId, appliedCode).catch(() => null);
          setQuote(refreshed ?? quote);
        }
      } else if (status === 403) {
        toast.error('This hold does not belong to you.');
      } else {
        toast.error('Something went wrong. Please try again.');
      }
    } finally {
      setSubmitting(false);
    }
  };

  const handlePaymentResolved = () => {
    if (intent) {
      void navigate(`/orders/${intent.orderId}`);
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

        <div style={{ marginTop: 16, marginBottom: 20 }}>
          <Typography.Text strong style={{ display: 'block', marginBottom: 6 }}>
            Discount code
          </Typography.Text>
          {appliedCode ? (
            <Space style={{ marginBottom: 12 }}>
              <Tag icon={<TagOutlined />} color="success">
                {appliedCode}
              </Tag>
              <Button
                size="small"
                type="link"
                disabled={intent !== null || applyingPromo}
                onClick={handleRemovePromo}
              >
                Remove
              </Button>
            </Space>
          ) : (
            <Space.Compact style={{ width: '100%', marginBottom: 12 }}>
              <Input
                placeholder="Have a code?"
                value={promoInput}
                maxLength={50}
                disabled={intent !== null}
                onChange={(event) => setPromoInput(event.target.value)}
                onPressEnter={() => void handleApplyPromo(promoInput)}
              />
              <Button
                loading={applyingPromo}
                disabled={intent !== null || promoInput.trim().length === 0}
                onClick={() => void handleApplyPromo(promoInput)}
              >
                Apply
              </Button>
            </Space.Compact>
          )}

          {publicCodes.length > 0 && !appliedCode && (
            <Select
              placeholder="…or pick an available offer"
              style={{ width: '100%', marginBottom: 12 }}
              disabled={intent !== null || applyingPromo}
              value={null}
              onChange={(code: string) => {
                setPromoInput(code);
                void handleApplyPromo(code);
              }}
              options={publicCodes.map((offer) => ({
                value: offer.code,
                label: `${offer.code} — ${
                  offer.discountType === 'Percentage'
                    ? `${offer.discountValue}% off`
                    : `${formatMoney(Math.round(offer.discountValue * 100), currency)} off`
                }${offer.description ? ` (${offer.description})` : ''}`,
              }))}
            />
          )}

          {quote ? (
            <>
              <PriceRow label="Subtotal" amountMinor={quote.subtotalMinor} currency={currency} />
              {quote.discountMinor > 0 && (
                <PriceRow
                  label={`Discount${appliedCode ? ` (${appliedCode})` : ''}`}
                  amountMinor={-quote.discountMinor}
                  currency={currency}
                  emphasis="success"
                />
              )}
              {quote.bookingFeeMinor > 0 && (
                <PriceRow
                  label="Booking fee"
                  amountMinor={quote.bookingFeeMinor}
                  currency={currency}
                />
              )}
              {quote.taxMinor > 0 && (
                <PriceRow
                  label={quote.taxLabel ?? 'Tax'}
                  amountMinor={quote.taxMinor}
                  currency={currency}
                />
              )}
            </>
          ) : null}

          <div
            style={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'baseline',
              marginTop: 12,
            }}
          >
            <Typography.Text strong style={{ fontSize: 16 }}>
              Total
            </Typography.Text>
            <Typography.Title level={3} style={{ margin: 0 }}>
              {formatMoney(quote?.totalMinor ?? hold.totalMinor, currency)}
            </Typography.Title>
          </div>
        </div>

        <Typography.Text strong style={{ display: 'block', marginBottom: 6 }}>
          Email for ticket delivery
        </Typography.Text>
        <Input
          prefix={<MailOutlined />}
          type="email"
          placeholder="you@example.com"
          value={buyerEmail}
          maxLength={320}
          disabled={intent !== null}
          onChange={(event) => setBuyerEmail(event.target.value)}
          style={{ marginBottom: 20 }}
        />

        {intent ? (
          isStripeConfigured && stripePromise ? (
            <Elements stripe={stripePromise} options={{ clientSecret: intent.clientSecret }}>
              <CheckoutPaymentForm
                holdId={hold.holdId}
                orderId={intent.orderId}
                expired={expired}
                emailValid={EMAIL_PATTERN.test(buyerEmail)}
                onResolved={handlePaymentResolved}
              />
            </Elements>
          ) : (
            <Alert
              type="error"
              showIcon
              message="Payment could not be set up (Stripe is not configured for this app). Please try again shortly."
            />
          )
        ) : (
          /* Sticky rather than inline: on a phone the summary, promo box and email field push
             this below the fold, and the one action the page exists for should never need
             hunting for. It un-sticks by itself once the card fits the screen. */
          <div
            style={{
              position: 'sticky',
              bottom: 0,
              paddingTop: 12,
              background: 'inherit',
            }}
          >
            <Button
              type="primary"
              size="large"
              block
              disabled={expired || !EMAIL_PATTERN.test(buyerEmail)}
              loading={submitting}
              onClick={() => void handleStartCheckout()}
            >
              {expired ? 'Hold expired' : 'Continue to payment'}
            </Button>
          </div>
        )}
      </Card>
    </div>
  );
}
