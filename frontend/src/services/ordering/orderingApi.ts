import { httpClient } from '../http/client';

/** Lifecycle status of an order. */
export type OrderStatus = 'Pending' | 'AwaitingPayment' | 'Confirmed' | 'Failed' | 'Refunded';

/**
 * One line of an order — either a reserved seat (`seatId` set, `quantity` always 1) or a
 * general-admission quantity (`generalAdmissionAllocationId` set), never both.
 */
export interface OrderLineResponse {
  seatId: string | null;
  generalAdmissionAllocationId: string | null;
  quantity: number;
  unitPriceMinor: number;
  priceMinor: number;
}

/** Read model for a single order. */
export interface OrderResponse {
  id: string;
  status: OrderStatus;
  totalMinor: number;
  /** Sum of the lines' prices, before discount or tax. */
  subtotalMinor: number;
  /** What the promo code took off. Zero when none was applied. */
  discountMinor: number;
  /** Tax charged on the post-discount subtotal. Zero for an untaxed event. */
  taxMinor: number;
  /** Display name for the tax (e.g. `"GST 18%"`), or `null` when untaxed. */
  taxLabel: string | null;
  /** The promo code that was actually applied, or `null`. */
  promoCode: string | null;
  currency: string;
  catalogEventId: string;
  holdId: string;
  lines: OrderLineResponse[];
  /**
   * The Stripe PaymentIntent client secret, while the order is awaiting payment — lets a buyer who
   * reloads mid-authentication (or the redirect-return page) resume Payment Element without a fresh
   * checkout call. `null` once the order reaches a terminal status.
   */
  paymentClientSecret: string | null;
}

/** Read model for one order in a list (no lines). */
export interface OrderSummaryResponse {
  id: string;
  status: OrderStatus;
  totalMinor: number;
  currency: string;
  catalogEventId: string;
  createdAt: string;
}

/** Paginated read model for a page of orders. */
export interface OrderListResponse {
  orders: OrderSummaryResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
}

/**
 * Checks out a hold. `idempotencyKey` should be generated once per checkout attempt and reused
 * across retries of that same attempt (never generated fresh on each retry).
 *
 * The backend creates (but does not confirm) a Stripe PaymentIntent as part of this call — a
 * `clientSecret` comes back for the frontend to mount Payment Element against, or `null` when the
 * payment already resolved synchronously (the no-Stripe-configured dev fallback).
 */
export async function checkout(
  holdId: string,
  idempotencyKey: string,
  buyerEmail: string,
  promoCode?: string | null,
): Promise<{ orderId: string; clientSecret: string | null }> {
  const response = await httpClient.post<{ orderId: string; clientSecret: string | null }>(
    '/api/ordering/v1/checkout',
    { holdId, buyerEmail, promoCode: promoCode ?? null },
    { headers: { 'Idempotency-Key': idempotencyKey } },
  );
  return response.data;
}

/** What a checkout would cost right now — see {@link quoteCheckout}. */
export interface CheckoutQuoteResponse {
  subtotalMinor: number;
  discountMinor: number;
  taxMinor: number;
  totalMinor: number;
  currency: string;
  taxLabel: string | null;
  /** The code that was accepted, or `null`. */
  promoCodeApplied: string | null;
  /**
   * Why a supplied code was not accepted (`NotFound`, `Expired`, `RedemptionLimitReached`, …), or
   * `null`. A rejected code is not an error — the quote is still valid, just undiscounted.
   */
  promoCodeRejection: string | null;
}

/**
 * Prices a hold without creating anything — what the buyer's "Apply" button calls. The real charge
 * re-prices server-side at confirm time, so this is advisory: a code that expires in between is
 * caught there, not silently honoured.
 */
export async function quoteCheckout(
  holdId: string,
  promoCode?: string | null,
): Promise<CheckoutQuoteResponse> {
  const response = await httpClient.post<CheckoutQuoteResponse>('/api/ordering/v1/checkout/quote', {
    holdId,
    promoCode: promoCode ?? null,
  });
  return response.data;
}

/** Fetches a single order. */
export async function getOrder(id: string): Promise<OrderResponse> {
  const response = await httpClient.get<OrderResponse>(`/api/ordering/v1/orders/${id}`);
  return response.data;
}

/**
 * Tells the backend to reconcile this order's payment with Stripe *now*, instead of waiting for
 * Stripe's webhook (which can't reach a developer machine) or the checkout saga's slower poll.
 * Call it the moment `confirmPayment` resolves — the browser knows the payment succeeded before
 * anything server-side does. Purely a nudge: the backend re-reads the real state from Stripe, so
 * this can't fake a payment. Safe to ignore failures — the saga still resolves on its own.
 */
export async function syncOrderPayment(id: string): Promise<void> {
  await httpClient.post(`/api/ordering/v1/orders/${id}/payment/sync`);
}

/**
 * Cancels a confirmed order: voids its tickets, releases the inventory, and refunds the payment.
 * Only a `Confirmed` order with no already-checked-in tickets can be cancelled.
 */
export async function cancelOrder(id: string): Promise<void> {
  await httpClient.post(`/api/ordering/v1/orders/${id}/cancel`);
}

/** Lists the caller's own orders (buyer). */
export async function listMyOrders(params: {
  page?: number;
  pageSize?: number;
}): Promise<OrderListResponse> {
  const response = await httpClient.get<OrderListResponse>('/api/ordering/v1/orders', {
    params: { ...params, mine: true },
  });
  return response.data;
}

/** Lists the caller's tenant's orders (organizer). */
export async function listTenantOrders(params: {
  page?: number;
  pageSize?: number;
}): Promise<OrderListResponse> {
  const response = await httpClient.get<OrderListResponse>('/api/ordering/v1/orders', {
    params: { ...params, forTenant: true },
  });
  return response.data;
}
