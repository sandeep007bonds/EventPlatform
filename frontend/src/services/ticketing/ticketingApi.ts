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
  /** The event — the whole run the ticket belongs to. */
  catalogEventId: string;
  /** The performance it admits to. A scan is validated against this, never the event. */
  eventSessionId: string;
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

/**
 * Fetches every ticket for one of a tenant's performances — e.g. to overlay check-in status on a
 * seat map. Per performance, because that is the grain the seat map is rendered at.
 */
export async function getSessionTickets(eventSessionId: string): Promise<TicketResponse[]> {
  const response = await httpClient.get<TicketResponse[]>(
    `/api/ticketing/v1/sessions/${eventSessionId}/tickets`,
  );
  return response.data;
}

/**
 * Fetches a ticket's QR code as a PNG and returns a local blob URL for it (suitable for an
 * `<img src>`). The endpoint requires auth (the ticket's own buyer, or the owning tenant), so this
 * goes through the shared `httpClient` — a plain `<img>` tag can't attach the bearer token itself.
 * Callers should `URL.revokeObjectURL` the result once it's no longer needed.
 */
export async function getTicketQrCodeUrl(ticketId: string): Promise<string> {
  const response = await httpClient.get(`/api/ticketing/v1/tickets/${ticketId}/qrcode`, {
    responseType: 'blob',
  });
  return URL.createObjectURL(response.data as Blob);
}

/**
 * Scans/checks in a ticket by its opaque token (as read from its QR code), for the given
 * **performance** and (optionally) a specific physical gate — omitting `gateId` means an unscoped
 * "master" scanner that bypasses any section-level gate restriction.
 *
 * The performance, not the event: at a three-night run, tonight's door must turn away tomorrow's
 * ticket. Throws (via axios) on `404` (no ticket matches that token, or it is for a different
 * performance — deliberately the same response, so presenting a valid ticket on the wrong night
 * does not confirm it is valid on another one) or `409` (already checked in or void, outside this
 * performance's check-in window, or presented at the wrong gate).
 */
export async function scanTicket(
  token: string,
  eventSessionId: string,
  gateId?: string,
): Promise<TicketResponse> {
  const response = await httpClient.post<TicketResponse>('/api/ticketing/v1/tickets/scan', {
    token,
    eventSessionId,
    gateId,
  });
  return response.data;
}
