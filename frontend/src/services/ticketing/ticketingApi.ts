import { httpClient } from '../http/client';

/** Lifecycle status of a ticket. */
export type TicketStatus = 'Issued' | 'CheckedIn' | 'Void';

/** Read model for a single ticket. */
export interface TicketResponse {
  id: string;
  orderId: string;
  catalogEventId: string;
  seatId: string;
  token: string;
  status: TicketStatus;
  issuedAt: string;
}

/**
 * Fetches the tickets for an order. Ticketing is populated asynchronously (pub/sub off
 * `OrderConfirmed`), so this can return an empty list briefly right after checkout — callers
 * should poll rather than treat an empty list as final.
 */
export async function getOrderTickets(orderId: string): Promise<TicketResponse[]> {
  const response = await httpClient.get<TicketResponse[]>(
    `/api/ticketing/v1/orders/${orderId}/tickets`,
  );
  return response.data;
}
