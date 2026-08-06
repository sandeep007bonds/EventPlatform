import { httpClient } from '../http/client';

/** Lifecycle status of a catalog event. */
export type EventStatus = 'Draft' | 'Published' | 'OnSale' | 'SoldOut' | 'Cancelled' | 'Completed';

/** An open-ended (platform, URL) social link — no fixed platform list. */
export interface SocialLinkResponse {
  platform: string;
  url: string;
}

/** A social link to save — same shape as {@link SocialLinkResponse}. */
export type SocialLinkInput = SocialLinkResponse;

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
  endsAt: string;
  doorsOpenAt: string | null;
  onSaleAt: string | null;
  bookingEndsAt: string | null;
  maxTicketsPerBuyer: number | null;
  requiresQueue: boolean;
  salesPaused: boolean;
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
  contactPhone: string | null;
  contactMobile: string | null;
  contactEmail: string | null;
  websiteUrl: string | null;
  socialLinks: SocialLinkResponse[];
}

/** Fields settable via {@link updateEventDetails} — Draft-only. `endsAt` is required. */
export interface UpdateEventDetailsRequest {
  description?: string | null;
  category?: string | null;
  endsAt: string;
  doorsOpenAt?: string | null;
  onSaleAt?: string | null;
  bookingEndsAt?: string | null;
  maxTicketsPerBuyer?: number | null;
  requiresQueue?: boolean;
  ageRestriction?: string | null;
  bannerImageUrl?: string | null;
  videoUrl?: string | null;
  contactPhone?: string | null;
  contactMobile?: string | null;
  contactEmail?: string | null;
  websiteUrl?: string | null;
  socialLinks?: SocialLinkInput[];
}

/** Read model for a single event group (tour). */
export interface EventGroupResponse {
  id: string;
  title: string;
  startsAt: string | null;
  endsAt: string | null;
  contactPhone: string | null;
  contactMobile: string | null;
  contactEmail: string | null;
  websiteUrl: string | null;
  socialLinks: SocialLinkResponse[];
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

/** Fields settable via {@link updateEventGroup} — all optional except title. */
export interface UpdateEventGroupRequest {
  title: string;
  startsAt?: string | null;
  endsAt?: string | null;
  contactPhone?: string | null;
  contactMobile?: string | null;
  contactEmail?: string | null;
  websiteUrl?: string | null;
  socialLinks?: SocialLinkInput[];
}

/** Paginated read model for a page of events. */
export interface ListEventsResponse {
  events: EventResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
}

/** A single reserved seat in a seat map. */
export interface SeatResponse {
  id: string;
  section: string;
  priceTier: string;
  priceAmount: number;
  row: string;
  number: number;
  label: string;
  entryGateId: string | null;
}

/** A general-admission (capacity-only, no individual seats) section of a seat map. */
export interface GeneralAdmissionSectionResponse {
  id: string;
  sectionName: string;
  priceTier: string;
  priceAmount: number;
  capacity: number;
  entryGateId: string | null;
}

/** Read model for an event's seat map — reserved seats and/or general-admission sections. */
export interface SeatMapResponse {
  eventId: string;
  name: string;
  capacity: number;
  seats: SeatResponse[];
  generalAdmissionSections: GeneralAdmissionSectionResponse[];
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

/** Fields for creating a new draft event. `endsAt` is required alongside `startsAt`. */
export interface CreateEventRequest {
  title: string;
  startsAt: string;
  endsAt: string;
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
  maxTicketsPerBuyer?: number | null;
  requiresQueue?: boolean;
}

/** Creates a new draft event for the caller's tenant. */
export async function createEvent(request: CreateEventRequest): Promise<{ id: string }> {
  const response = await httpClient.post<{ id: string }>('/api/catalog/v1/events', request);
  return response.data;
}

/** Per-section allocation choice: individually-seated rows, or a capacity-only pool. */
export type AllocationType = 'Reserved' | 'GeneralAdmission';

/**
 * A seat-map section to create — `Reserved` generates rows × seatsPerRow seats;
 * `GeneralAdmission` is a capacity-only pool with no individual seats.
 */
export interface SeatMapSectionInput {
  name: string;
  priceTier: string;
  priceAmount: number;
  allocationType: AllocationType;
  rows?: number;
  seatsPerRow?: number;
  capacity?: number;
  entryGateId?: string | null;
}

/** A named physical entry point at an event's location. */
export interface EntryGateResponse {
  id: string;
  eventId: string;
  name: string;
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

/** Adds more sections to a draft event's existing seat map (Draft-only). */
export async function addSeatMapSections(
  eventId: string,
  request: { sections: SeatMapSectionInput[] },
): Promise<{ seatMapId: string }> {
  const response = await httpClient.post<{ seatMapId: string }>(
    `/api/catalog/v1/events/${eventId}/seatmap/sections`,
    request,
  );
  return response.data;
}

/** Replaces one existing section of a draft event's seat map (Draft-only) — a full remove+re-add. */
export async function updateSeatMapSection(
  eventId: string,
  request: { currentSectionName: string; section: SeatMapSectionInput },
): Promise<{ seatMapId: string }> {
  const response = await httpClient.put<{ seatMapId: string }>(
    `/api/catalog/v1/events/${eventId}/seatmap/sections`,
    request,
  );
  return response.data;
}

/** Removes one section from a draft event's existing seat map entirely (Draft-only). */
export async function removeSeatMapSection(eventId: string, sectionName: string): Promise<void> {
  await httpClient.delete(
    `/api/catalog/v1/events/${eventId}/seatmap/sections/${encodeURIComponent(sectionName)}`,
  );
}

/** Publishes a draft event, making it sellable. */
export async function publishEvent(eventId: string): Promise<void> {
  await httpClient.post(`/api/catalog/v1/events/${eventId}/publish`);
}

/** Pauses sales for a published event, without affecting already-placed holds/tickets. */
export async function pauseSales(eventId: string): Promise<void> {
  await httpClient.post(`/api/catalog/v1/events/${eventId}/pause-sales`);
}

/** Resumes sales for a published event previously paused via {@link pauseSales}. */
export async function resumeSales(eventId: string): Promise<void> {
  await httpClient.post(`/api/catalog/v1/events/${eventId}/resume-sales`);
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

/** Updates an event group (tour) — dates and contact/social defaults for its legs. */
export async function updateEventGroup(
  id: string,
  request: UpdateEventGroupRequest,
): Promise<void> {
  await httpClient.put(`/api/catalog/v1/event-groups/${id}`, request);
}

/** Lists every entry gate defined for an event. Public — no login required. */
export async function listEntryGates(eventId: string): Promise<EntryGateResponse[]> {
  const response = await httpClient.get<EntryGateResponse[]>(
    `/api/catalog/v1/events/${eventId}/entry-gates`,
  );
  return response.data;
}

/** Defines a new entry gate for an event. */
export async function createEntryGate(eventId: string, name: string): Promise<{ id: string }> {
  const response = await httpClient.post<{ id: string }>(
    `/api/catalog/v1/events/${eventId}/entry-gates`,
    { name },
  );
  return response.data;
}
