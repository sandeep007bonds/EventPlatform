import { loadStripe, type Stripe } from '@stripe/stripe-js';

const publishableKey = import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY;

/** Whether a real Stripe publishable key is configured for this build. */
export const isStripeConfigured = Boolean(publishableKey);

/**
 * The shared Stripe.js instance, loaded once at module scope (per Stripe's own guidance — never
 * call `loadStripe` on every render). `null` when no publishable key is configured, so
 * `CheckoutPage` never mounts `<Elements>`/`<CardElement>` and never loads Stripe.js at all.
 */
export const stripePromise: Promise<Stripe | null> | null = isStripeConfigured
  ? loadStripe(publishableKey)
  : null;

/**
 * Dev/CI fallback payment-method id, submitted when no publishable key is configured — the exact
 * value `StripePaymentGateway` used to hardcode server-side, still a valid Stripe test token
 * against a test-mode secret key. Keeps local checkout working with zero required Stripe setup.
 */
export const DEV_FALLBACK_PAYMENT_METHOD_ID = 'pm_card_visa';
