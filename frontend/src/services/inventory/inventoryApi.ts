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

/** One general-admission allocation's current status, keyed by the Catalog section id a buyer already has. */
export interface GeneralAdmissionAllocationResponse {
  allocationId: string;
  catalogSectionId: string;
  remaining: number;
  totalCapacity: number;
  heldCount: number;
  soldCount: number;
}

/** Fetches every general-admission allocation for an event — the real ids a hold request must reference. */
export async function getGeneralAdmissionAllocations(
  eventId: string,
): Promise<GeneralAdmissionAllocationResponse[]> {
  const response = await httpClient.get<GeneralAdmissionAllocationResponse[]>(
    `/api/inventory/v1/events/${eventId}/inventory/general-admission`,
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

/**
 * One line of a placed or fetched hold — either a reserved seat (`seatId` set, `quantity` always 1)
 * or a general-admission quantity (`generalAdmissionAllocationId` set), never both.
 */
export interface HoldLineView {
  inventoryItemId: string | null;
  seatId: string | null;
  generalAdmissionAllocationId: string | null;
  quantity: number;
  priceTier: string;
  unitPriceMinor: number;
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

/** A requested quantity of one general-admission allocation, as part of a hold request. */
export interface GeneralAdmissionSelection {
  allocationId: string;
  quantity: number;
}

/**
 * Places a hold on the given seats and/or general-admission quantities. Throws (409, via axios) if
 * a seat or allocation is no longer available, or the event's booking window has closed.
 */
export async function placeHold(request: {
  eventId: string;
  seatIds?: string[];
  generalAdmissionSelections?: GeneralAdmissionSelection[];
  queueAdmissionToken?: string;
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
