import { httpClient } from '../http/client';

/** Lifecycle status of an order. */
export type OrderStatus = 'Pending' | 'AwaitingPayment' | 'Confirmed' | 'Failed' | 'Refunded';

/** One line of an order. */
export interface OrderLineResponse {
  seatId: string;
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
 */
export async function checkout(
  holdId: string,
  idempotencyKey: string,
): Promise<{ orderId: string }> {
  const response = await httpClient.post<{ orderId: string }>(
    '/api/ordering/v1/checkout',
    { holdId },
    { headers: { 'Idempotency-Key': idempotencyKey } },
  );
  return response.data;
}

/** Fetches a single order. */
export async function getOrder(id: string): Promise<OrderResponse> {
  const response = await httpClient.get<OrderResponse>(`/api/ordering/v1/orders/${id}`);
  return response.data;
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
