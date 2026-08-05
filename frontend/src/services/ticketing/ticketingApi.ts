import { httpClient } from '../http/client';

/** Lifecycle status of a ticket. */
export type TicketStatus = 'Issued' | 'CheckedIn' | 'Void';

/**
 * Read model for a single ticket — either a reserved seat (`seatId` set) or a general-admission
 * unit (`generalAdmissionAllocationId` set), never both.
 */
export interface TicketResponse {
  id: string;
  orderId: string;
  catalogEventId: string;
  seatId: string | null;
  generalAdmissionAllocationId: string | null;
  token: string;
  status: TicketStatus;
  issuedAt: string;
  checkedInAt: string | null;
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

/** Fetches every ticket for a tenant's event — e.g. to overlay check-in status on a seat map. */
export async function getEventTickets(eventId: string): Promise<TicketResponse[]> {
  const response = await httpClient.get<TicketResponse[]>(
    `/api/ticketing/v1/events/${eventId}/tickets`,
  );
  return response.data;
}

/**
 * Scans/checks in a ticket by its opaque token (as read from its QR code), for the given event
 * and (optionally) a specific physical gate — omitting `gateId` means an unscoped "master"
 * scanner that bypasses any section-level gate restriction. Throws (via axios) on `404` (no
 * ticket matches that token, or it's not for the selected event) or `409` (already checked in or
 * void, outside the check-in window, or presented at the wrong gate).
 */
export async function scanTicket(
  token: string,
  eventId: string,
  gateId?: string,
): Promise<TicketResponse> {
  const response = await httpClient.post<TicketResponse>('/api/ticketing/v1/tickets/scan', {
    token,
    eventId,
    gateId,
  });
  return response.data;
}
