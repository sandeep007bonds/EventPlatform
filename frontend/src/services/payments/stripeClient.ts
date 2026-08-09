import { loadStripe, type Stripe } from '@stripe/stripe-js';

const publishableKey = import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY;

/** Whether a real Stripe publishable key is configured for this build. */
export const isStripeConfigured = Boolean(publishableKey);

/**
 * The shared Stripe.js instance, loaded once at module scope (per Stripe's own guidance — never
 * call `loadStripe` on every render). `null` when no publishable key is configured, so
 * `CheckoutPage` never mounts `<Elements>`/`<PaymentElement>` and never loads Stripe.js at all.
 */
export const stripePromise: Promise<Stripe | null> | null = isStripeConfigured
  ? loadStripe(publishableKey)
  : null;
