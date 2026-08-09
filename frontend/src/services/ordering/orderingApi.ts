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
): Promise<{ orderId: string; clientSecret: string | null }> {
  const response = await httpClient.post<{ orderId: string; clientSecret: string | null }>(
    '/api/ordering/v1/checkout',
    { holdId, buyerEmail },
    { headers: { 'Idempotency-Key': idempotencyKey } },
  );
  return response.data;
}

/** Fetches a single order. */
export async function getOrder(id: string): Promise<OrderResponse> {
  const response = await httpClient.get<OrderResponse>(`/api/ordering/v1/orders/${id}`);
  return response.data;
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
