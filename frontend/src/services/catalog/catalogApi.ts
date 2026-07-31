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
  venueId: string;
  description: string | null;
  category: string | null;
  endsAt: string | null;
  doorsOpenAt: string | null;
  onSaleAt: string | null;
  offSaleAt: string | null;
  ageRestriction: string | null;
  bannerImageUrl: string | null;
  videoUrl: string | null;
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

/** Read model for a single venue. */
export interface VenueResponse {
  id: string;
  name: string;
  addressLine1: string;
  addressLine2: string | null;
  city: string;
  region: string | null;
  postalCode: string | null;
  country: string;
  latitude: number | null;
  longitude: number | null;
  capacity: number | null;
}

/** Paginated read model for a page of venues. */
export interface ListVenuesResponse {
  venues: VenueResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
}

/** Fields for creating or updating a venue. */
export interface VenueRequest {
  name: string;
  addressLine1: string;
  addressLine2?: string | null;
  city: string;
  region?: string | null;
  postalCode?: string | null;
  country: string;
  latitude?: number | null;
  longitude?: number | null;
  capacity?: number | null;
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

/** Sets a draft event's descriptive/promotional details (Draft-only; 409 otherwise). */
export async function updateEventDetails(
  eventId: string,
  request: UpdateEventDetailsRequest,
): Promise<void> {
  await httpClient.put(`/api/catalog/v1/events/${eventId}/details`, request);
}

/** Fetches a single venue. 404s if it doesn't exist. Public — no login required. */
export async function getVenue(id: string): Promise<VenueResponse> {
  const response = await httpClient.get<VenueResponse>(`/api/catalog/v1/venues/${id}`);
  return response.data;
}

/** Lists the caller's own venues — an organizer's reusable-venue picker, not public browsing. */
export async function listVenues(params: {
  page?: number;
  pageSize?: number;
}): Promise<ListVenuesResponse> {
  const response = await httpClient.get<ListVenuesResponse>('/api/catalog/v1/venues', { params });
  return response.data;
}

/** Creates a new venue for the caller's tenant. */
export async function createVenue(request: VenueRequest): Promise<{ id: string }> {
  const response = await httpClient.post<{ id: string }>('/api/catalog/v1/venues', request);
  return response.data;
}

/** Updates an existing venue the caller's tenant owns. */
export async function updateVenue(id: string, request: VenueRequest): Promise<void> {
  await httpClient.put(`/api/catalog/v1/venues/${id}`, request);
}
