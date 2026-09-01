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
  /** URL-safe public identifier — the `/events/{slug}` a buyer sees instead of a GUID. */
  slug: string;
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
  /** Sales-tax rate as a percentage (e.g. 18 for 18% GST); null when the event is untaxed. */
  taxRatePercent: number | null;
  /** Display name for the tax on a receipt (e.g. "GST 18%"). */
  taxLabel: string | null;
  /** Booking fee per ticket in minor units (e.g. 3000 for ₹30); 0 means no fee. */
  bookingFeePerTicketMinor: number;
  /** The venue's IANA time zone (e.g. "Asia/Kolkata"), or null when not set. */
  timeZoneId: string | null;
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

/**
 * Fields settable via {@link updateEventDetails} — Draft-only (409 otherwise).
 *
 * Everything here changes what a ticket holder bought, which is why it locks at publish. Title,
 * description, imagery and contact details are not here; they go through
 * {@link updateEventPresentation}, which works at any status.
 */
export interface UpdateEventDetailsRequest {
  startsAt: string;
  endsAt: string;
  doorsOpenAt?: string | null;
  onSaleAt?: string | null;
  bookingEndsAt?: string | null;
  locationName: string;
  addressLine1: string;
  addressLine2?: string | null;
  city: string;
  region?: string | null;
  postalCode?: string | null;
  country: string;
  latitude?: number | null;
  longitude?: number | null;
  maxTicketsPerBuyer?: number | null;
  requiresQueue?: boolean;
  taxRatePercent?: number | null;
  taxLabel?: string | null;
  bookingFeePerTicketMinor?: number;
  timeZoneId?: string | null;
}

/** Fields settable via {@link updateEventPresentation} — editable at any status. */
export interface UpdateEventPresentationRequest {
  title: string;
  description?: string | null;
  category?: string | null;
  ageRestriction?: string | null;
  bannerImageUrl?: string | null;
  videoUrl?: string | null;
  contactPhone?: string | null;
  contactMobile?: string | null;
  contactEmail?: string | null;
  websiteUrl?: string | null;
  socialLinks?: SocialLinkInput[];
}

/** The kinds of legal document an organizer publishes. */
export type PolicyKind = 'Terms' | 'Privacy' | 'Refund';

/** One resolved policy document. */
export interface PolicyDocumentResponse {
  kind: PolicyKind;
  /** Sanitised HTML — safe to render, but still the organizer's own words. */
  bodyHtml: string;
  /** Revision number in force; captured on an order so a dispute can name what was agreed. */
  version: number;
  updatedAt: string;
  /** True when this event overrides the organizer's tenant-wide default. */
  isEventOverride: boolean;
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
  taxRatePercent?: number | null;
  taxLabel?: string | null;
  bookingFeePerTicketMinor?: number;
  timeZoneId?: string | null;
  /** Optional vanity URL slug; derived from the title when omitted. */
  slug?: string | null;
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

/** Sets a draft event's dates, venue and pricing rules (Draft-only; 409 otherwise). */
export async function updateEventDetails(
  eventId: string,
  request: UpdateEventDetailsRequest,
): Promise<void> {
  await httpClient.put(`/api/catalog/v1/events/${eventId}/details`, request);
}

/** Sets how an event is presented. Accepted at any status, including after publish. */
export async function updateEventPresentation(
  eventId: string,
  request: UpdateEventPresentationRequest,
): Promise<void> {
  await httpClient.put(`/api/catalog/v1/events/${eventId}/presentation`, request);
}

/**
 * Changes a draft event's public slug. 409 once published — the URL has already been shared — and
 * 409 if another event holds it.
 */
export async function changeEventSlug(eventId: string, slug: string): Promise<void> {
  await httpClient.put(`/api/catalog/v1/events/${eventId}/slug`, { slug });
}

/** Fetches an event by its public slug. Same read model and visibility rule as {@link getEvent}. */
export async function getEventBySlug(slug: string): Promise<EventResponse> {
  const response = await httpClient.get<EventResponse>(`/api/catalog/v1/events/by-slug/${slug}`);
  return response.data;
}

/**
 * The policy documents in force for one event — the organizer's defaults, with this event's own
 * overrides substituted in. Anonymous: a buyer reads the refund policy before deciding to buy.
 */
export async function getEventPolicies(eventId: string): Promise<PolicyDocumentResponse[]> {
  const response = await httpClient.get<PolicyDocumentResponse[]>(
    `/api/catalog/v1/events/${eventId}/policies`,
  );
  return response.data;
}

/** The organizer's own tenant-wide default documents, used wherever an event sets no override. */
export async function getTenantPolicies(): Promise<PolicyDocumentResponse[]> {
  const response = await httpClient.get<PolicyDocumentResponse[]>('/api/catalog/v1/policies');
  return response.data;
}

/** Writes the organizer's tenant-wide default for one kind of document. */
export async function setTenantPolicy(
  kind: PolicyKind,
  bodyHtml: string,
): Promise<{ version: number }> {
  const response = await httpClient.put<{ version: number }>(`/api/catalog/v1/policies/${kind}`, {
    bodyHtml,
  });
  return response.data;
}

/** Writes one event's override of a document, replacing the tenant default for that event. */
export async function setEventPolicy(
  eventId: string,
  kind: PolicyKind,
  bodyHtml: string,
): Promise<{ version: number }> {
  const response = await httpClient.put<{ version: number }>(
    `/api/catalog/v1/events/${eventId}/policies/${kind}`,
    { bodyHtml },
  );
  return response.data;
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

/** How a promo code's value is interpreted. */
export type DiscountType = 'Percentage' | 'FixedAmount';

/** A discount code, as the owning organizer sees it — including caps and inactive codes. */
export interface PromoCodeResponse {
  id: string;
  eventId: string;
  code: string;
  description: string | null;
  discountType: DiscountType;
  /** A percentage in (0, 100] for Percentage, or a flat amount in major units for FixedAmount. */
  discountValue: number;
  validFrom: string | null;
  validTo: string | null;
  isPublic: boolean;
  maxRedemptions: number | null;
  maxRedemptionsPerBuyer: number | null;
  isActive: boolean;
  createdAt: string;
  /** Tiers the code is restricted to. Empty means every tier. */
  priceTiers: string[];
}

/**
 * A promo code as a *buyer* sees it at checkout. Narrower than the organizer's view on purpose —
 * redemption caps are not published.
 */
export interface PublicPromoCodeResponse {
  code: string;
  description: string | null;
  discountType: DiscountType;
  discountValue: number;
  priceTiers: string[];
}

/** Body for creating a promo code. */
export interface CreatePromoCodeRequest {
  code: string;
  description?: string | null;
  discountType: DiscountType;
  discountValue: number;
  validFrom?: string | null;
  validTo?: string | null;
  isPublic?: boolean;
  maxRedemptions?: number | null;
  maxRedemptionsPerBuyer?: number | null;
  /** Omit or send [] to apply the code to every tier. */
  priceTiers?: string[];
}

/** Lists every promo code for an event, active or not. Organizer-only. */
export async function listPromoCodes(eventId: string): Promise<PromoCodeResponse[]> {
  const response = await httpClient.get<PromoCodeResponse[]>(
    `/api/catalog/v1/events/${eventId}/promo-codes`,
  );
  return response.data;
}

/** Lists the event's advertised, currently-redeemable codes. Public — no login required. */
export async function listPublicPromoCodes(eventId: string): Promise<PublicPromoCodeResponse[]> {
  const response = await httpClient.get<PublicPromoCodeResponse[]>(
    `/api/catalog/v1/events/${eventId}/promo-codes/public`,
  );
  return response.data;
}

/** Creates a promo code for an event. */
export async function createPromoCode(
  eventId: string,
  request: CreatePromoCodeRequest,
): Promise<{ id: string }> {
  const response = await httpClient.post<{ id: string }>(
    `/api/catalog/v1/events/${eventId}/promo-codes`,
    request,
  );
  return response.data;
}

/** Retires a promo code. There is no edit — deactivate and create another instead. */
export async function deactivatePromoCode(eventId: string, id: string): Promise<void> {
  await httpClient.post(`/api/catalog/v1/events/${eventId}/promo-codes/${id}/deactivate`);
}

/** A named, priced kind of ticket for an event. Seat-map sections are sold as one of these. */
export interface TicketTypeResponse {
  id: string;
  name: string;
  /** Price per ticket in minor currency units — divide by 100 for display. */
  priceMinor: number;
  description: string | null;
  /** Narrows the event's own on-sale window for this type only. */
  salesStartsAt: string | null;
  salesEndsAt: string | null;
  /** Cap for this type, on top of the event's overall per-buyer limit. */
  maxPerBuyer: number | null;
  sortOrder: number;
  isActive: boolean;
}

/** Body for creating a ticket type. */
export interface CreateTicketTypeRequest {
  name: string;
  priceMinor: number;
  description?: string | null;
  salesStartsAt?: string | null;
  salesEndsAt?: string | null;
  maxPerBuyer?: number | null;
  sortOrder?: number;
}

/**
 * Body for updating a ticket type.
 *
 * `priceMinor` is rejected with a 409 once the event is published: Inventory holds its own copy of
 * the price from provisioning time, so a change here would move the displayed price without moving
 * the charged one. Send the unchanged value on a published event.
 */
export type UpdateTicketTypeRequest = Required<
  Pick<CreateTicketTypeRequest, 'name' | 'priceMinor' | 'sortOrder'>
> &
  Pick<CreateTicketTypeRequest, 'description' | 'salesStartsAt' | 'salesEndsAt' | 'maxPerBuyer'>;

/** Lists an event's ticket types, active or not — the organizer's view. */
export async function listTicketTypes(eventId: string): Promise<TicketTypeResponse[]> {
  const response = await httpClient.get<TicketTypeResponse[]>(
    `/api/catalog/v1/events/${eventId}/ticket-types`,
  );
  return response.data;
}

/** Creates a ticket type. Allowed after publish, unlike the seat-map endpoints. */
export async function createTicketType(
  eventId: string,
  request: CreateTicketTypeRequest,
): Promise<{ id: string }> {
  const response = await httpClient.post<{ id: string }>(
    `/api/catalog/v1/events/${eventId}/ticket-types`,
    request,
  );
  return response.data;
}

/** Updates a ticket type. See {@link UpdateTicketTypeRequest} on repricing after publish. */
export async function updateTicketType(
  eventId: string,
  id: string,
  request: UpdateTicketTypeRequest,
): Promise<void> {
  await httpClient.put(`/api/catalog/v1/events/${eventId}/ticket-types/${id}`, request);
}

/** Retires a ticket type. Never deleted — seats and orders reference it by id. */
export async function deactivateTicketType(eventId: string, id: string): Promise<void> {
  await httpClient.post(`/api/catalog/v1/events/${eventId}/ticket-types/${id}/deactivate`);
}
