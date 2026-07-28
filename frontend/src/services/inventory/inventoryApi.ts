import { httpClient } from '../http/client';

/** Availability status of a single seat. */
export type SeatInventoryStatus = 'Available' | 'Held' | 'Sold' | 'Blocked';

/** A single seat's current availability status. */
export interface InventorySeatResponse {
  seatId: string;
  status: SeatInventoryStatus;
}

/** Fetches every seat's current status for an event. */
export async function getInventorySeats(eventId: string): Promise<InventorySeatResponse[]> {
  const response = await httpClient.get<InventorySeatResponse[]>(
    `/api/inventory/v1/events/${eventId}/inventory/seats`,
  );
  return response.data;
}

/** Provisioned seat count for an event. */
export async function getInventoryCount(
  eventId: string,
): Promise<{ eventId: string; seatCount: number }> {
  const response = await httpClient.get<{ eventId: string; seatCount: number }>(
    `/api/inventory/v1/events/${eventId}/inventory`,
  );
  return response.data;
}

/** One line of a placed or fetched hold. */
export interface HoldLineView {
  inventoryItemId: string;
  seatId: string;
  priceTier: string;
  priceMinor: number;
}

/** Read model for a hold. */
export interface HoldView {
  holdId: string;
  tenantId: string;
  catalogEventId: string;
  userId: string;
  status: 'Active' | 'Converted' | 'Released';
  expiresAt: string;
  totalMinor: number;
  lines: HoldLineView[];
}

/** Places a hold on the given seats. Throws (409, via axios) if any seat is no longer available. */
export async function placeHold(request: {
  eventId: string;
  seatIds: string[];
}): Promise<{ holdId: string; expiresAt: string }> {
  const response = await httpClient.post<{ holdId: string; expiresAt: string }>(
    '/api/inventory/v1/holds/',
    request,
  );
  return response.data;
}

/** Fetches a hold's current state. */
export async function getHold(holdId: string): Promise<HoldView> {
  const response = await httpClient.get<HoldView>(`/api/inventory/v1/holds/${holdId}`);
  return response.data;
}

/** Releases an active hold (e.g. the buyer navigates away before checking out). */
export async function releaseHold(holdId: string): Promise<void> {
  await httpClient.delete(`/api/inventory/v1/holds/${holdId}`);
}

/** Blocks seats so they can't be held or sold (e.g. a kill or a restricted view). */
export async function blockSeats(
  eventId: string,
  request: { seatIds: string[]; reason?: string },
): Promise<void> {
  await httpClient.post(`/api/inventory/v1/events/${eventId}/inventory/block`, request);
}

/** Unblocks previously blocked seats. */
export async function unblockSeats(eventId: string, request: { seatIds: string[] }): Promise<void> {
  await httpClient.post(`/api/inventory/v1/events/${eventId}/inventory/unblock`, request);
}
