import { httpClient } from '../http/client';

/** Lifecycle status of a catalog event. */
export type EventStatus = 'Draft' | 'Published' | 'OnSale' | 'SoldOut' | 'Cancelled' | 'Completed';

/** Read model for a single event. */
export interface EventResponse {
  id: string;
  title: string;
  startsAt: string;
  status: EventStatus;
  currency: string;
}

/** Paginated read model for a page of events. */
export interface ListEventsResponse {
  events: EventResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
}

/** A single seat in a seat map. */
export interface SeatResponse {
  id: string;
  section: string;
  priceTier: string;
  priceAmount: number;
  row: string;
  number: number;
  label: string;
}

/** Read model for an event's seat map. */
export interface SeatMapResponse {
  eventId: string;
  name: string;
  capacity: number;
  seats: SeatResponse[];
}

/** Lists events. Public browsing (default) or `mine: true` for the caller's own tenant dashboard. */
export async function listEvents(params: {
  status?: EventStatus;
  page?: number;
  pageSize?: number;
  mine?: boolean;
}): Promise<ListEventsResponse> {
  const response = await httpClient.get<ListEventsResponse>('/api/catalog/v1/events', { params });
  return response.data;
}

/** Fetches a single event. 404s if it doesn't exist or isn't visible to the caller. */
export async function getEvent(id: string): Promise<EventResponse> {
  const response = await httpClient.get<EventResponse>(`/api/catalog/v1/events/${id}`);
  return response.data;
}

/** Fetches an event's seat map. 404s if it doesn't exist or isn't visible to the caller. */
export async function getSeatMap(eventId: string): Promise<SeatMapResponse> {
  const response = await httpClient.get<SeatMapResponse>(
    `/api/catalog/v1/events/${eventId}/seatmap`,
  );
  return response.data;
}

/** Creates a new draft event for the caller's tenant. */
export async function createEvent(request: {
  venueId: string;
  title: string;
  startsAt: string;
  currency: string;
}): Promise<{ id: string }> {
  const response = await httpClient.post<{ id: string }>('/api/catalog/v1/events', request);
  return response.data;
}

/** A seat-map section to create, generating rows × seatsPerRow seats. */
export interface SeatMapSectionInput {
  name: string;
  priceTier: string;
  priceAmount: number;
  rows: number;
  seatsPerRow: number;
}

/** Defines the seat map for a draft event (one time only). */
export async function defineSeatMap(
  eventId: string,
  request: { name: string; sections: SeatMapSectionInput[] },
): Promise<{ seatMapId: string }> {
  const response = await httpClient.post<{ seatMapId: string }>(
    `/api/catalog/v1/events/${eventId}/seatmap`,
    request,
  );
  return response.data;
}

/** Publishes a draft event, making it sellable. */
export async function publishEvent(eventId: string): Promise<void> {
  await httpClient.post(`/api/catalog/v1/events/${eventId}/publish`);
}
