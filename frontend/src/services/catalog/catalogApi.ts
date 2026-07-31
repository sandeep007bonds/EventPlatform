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
  eventGroupId: string | null;
  description: string | null;
  category: string | null;
  endsAt: string | null;
  doorsOpenAt: string | null;
  onSaleAt: string | null;
  offSaleAt: string | null;
  ageRestriction: string | null;
  bannerImageUrl: string | null;
  videoUrl: string | null;
  locationName: string;
  addressLine1: string;
  addressLine2: string | null;
  city: string;
  region: string | null;
  postalCode: string | null;
  country: string;
  latitude: number | null;
  longitude: number | null;
}

/** Fields settable via {@link updateEventDetails} — all optional, Draft-only. */
export interface UpdateEventDetailsRequest {
  description?: string | null;
  category?: string | null;
  endsAt?: string | null;
  doorsOpenAt?: string | null;
  onSaleAt?: string | null;
  offSaleAt?: string | null;
  ageRestriction?: string | null;
  bannerImageUrl?: string | null;
  videoUrl?: string | null;
}

/** Read model for a single event group (tour). */
export interface EventGroupResponse {
  id: string;
  title: string;
}

/** Paginated read model for a page of event groups. */
export interface ListEventGroupsResponse {
  eventGroups: EventGroupResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
}

/** Fields for creating an event group (tour). */
export interface EventGroupRequest {
  title: string;
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

/**
 * Lists events. Public browsing (default) or `mine: true` for the caller's own tenant dashboard.
 * `eventGroupId` filters to the legs of a given tour, in either mode.
 */
export async function listEvents(params: {
  status?: EventStatus;
  page?: number;
  pageSize?: number;
  mine?: boolean;
  eventGroupId?: string;
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

/** Fields for creating a new draft event. */
export interface CreateEventRequest {
  title: string;
  startsAt: string;
  currency: string;
  locationName: string;
  addressLine1: string;
  addressLine2?: string | null;
  city: string;
  region?: string | null;
  postalCode?: string | null;
  country: string;
  latitude?: number | null;
  longitude?: number | null;
  eventGroupId?: string | null;
}

/** Creates a new draft event for the caller's tenant. */
export async function createEvent(request: CreateEventRequest): Promise<{ id: string }> {
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

/** Sets a draft event's descriptive/promotional details (Draft-only; 409 otherwise). */
export async function updateEventDetails(
  eventId: string,
  request: UpdateEventDetailsRequest,
): Promise<void> {
  await httpClient.put(`/api/catalog/v1/events/${eventId}/details`, request);
}

/** Fetches a single event group (tour). 404s if it doesn't exist. Public — no login required. */
export async function getEventGroup(id: string): Promise<EventGroupResponse> {
  const response = await httpClient.get<EventGroupResponse>(`/api/catalog/v1/event-groups/${id}`);
  return response.data;
}

/** Lists the caller's own event groups (tours) — an organizer's "pick or create a tour" picker. */
export async function listEventGroups(params: {
  page?: number;
  pageSize?: number;
}): Promise<ListEventGroupsResponse> {
  const response = await httpClient.get<ListEventGroupsResponse>('/api/catalog/v1/event-groups', {
    params,
  });
  return response.data;
}

/** Creates a new event group (tour) for the caller's tenant. */
export async function createEventGroup(request: EventGroupRequest): Promise<{ id: string }> {
  const response = await httpClient.post<{ id: string }>('/api/catalog/v1/event-groups', request);
  return response.data;
}
