import { httpClient } from '../http/client';

/** Availability status of a single seat. */
export type SeatInventoryStatus = 'Available' | 'Held' | 'Sold' | 'Blocked';

/**
 * A single seat's current availability, plus what it costs for this performance.
 *
 * The price comes from Inventory rather than being re-derived here from Catalog's ticket types:
 * this is the price that was actually provisioned, so a picker can never quote a number the
 * checkout then refuses (ADR-0034).
 */
export interface InventorySeatResponse {
  seatId: string;
  status: SeatInventoryStatus;
  ticketTypeId: string;
  priceMinor: number;
}

/**
 * Fetches every seat's current status for one **performance**. The same physical seat has a
 * different status on every night of a run, which is the whole reason this is keyed here and not
 * on the event (ADR-0039).
 */
export async function getInventorySeats(eventSessionId: string): Promise<InventorySeatResponse[]> {
  const response = await httpClient.get<InventorySeatResponse[]>(
    `/api/inventory/v1/sessions/${eventSessionId}/inventory/seats`,
  );
  return response.data;
}

/**
 * One general-admission allocation's current status. `admissionAreaId` is the **Venue** area it was
 * provisioned from, which is how a rendered map matches a pool to the block a buyer clicked.
 */
export interface GeneralAdmissionAllocationResponse {
  allocationId: string;
  admissionAreaId: string;
  ticketTypeId: string;
  /** The price of one admission, in minor units. */
  priceMinor: number;
  remaining: number;
  totalCapacity: number;
  heldCount: number;
  soldCount: number;
}

/**
 * Fetches every general-admission allocation for a performance — the real ids a hold request must
 * reference.
 */
export async function getGeneralAdmissionAllocations(
  eventSessionId: string,
): Promise<GeneralAdmissionAllocationResponse[]> {
  const response = await httpClient.get<GeneralAdmissionAllocationResponse[]>(
    `/api/inventory/v1/sessions/${eventSessionId}/inventory/general-admission`,
  );
  return response.data;
}

/** Provisioned seat count for a performance. */
export async function getInventoryCount(
  eventSessionId: string,
): Promise<{ eventSessionId: string; seatCount: number }> {
  const response = await httpClient.get<{ eventSessionId: string; seatCount: number }>(
    `/api/inventory/v1/sessions/${eventSessionId}/inventory`,
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
  /** The Catalog ticket type this line sells as, resolved at provisioning time. */
  ticketTypeId: string;
  unitPriceMinor: number;
  priceMinor: number;
}

/** Read model for a hold. */
export interface HoldView {
  holdId: string;
  tenantId: string;
  /** The event — the whole run. Promo codes and the per-buyer cap are decided at this level. */
  catalogEventId: string;
  /** The performance the held seats belong to. This is what the buyer actually chose. */
  eventSessionId: string;
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
 * Places a hold on the given seats and/or general-admission quantities of one performance. Throws
 * (409, via axios) if a seat or allocation is no longer available, that performance's booking
 * window has closed, sales are paused, the buyer is over their cap for the run, or the event needs
 * a queue admission token and none was presented.
 */
export async function placeHold(request: {
  eventSessionId: string;
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
  eventSessionId: string,
  request: { seatIds: string[]; reason?: string },
): Promise<void> {
  await httpClient.post(`/api/inventory/v1/sessions/${eventSessionId}/inventory/block`, request);
}

/** Unblocks previously blocked seats. */
export async function unblockSeats(
  eventSessionId: string,
  request: { seatIds: string[] },
): Promise<void> {
  await httpClient.post(`/api/inventory/v1/sessions/${eventSessionId}/inventory/unblock`, request);
}
