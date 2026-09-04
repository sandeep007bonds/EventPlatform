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
  status: EventStatus;
  currency: string;
  eventGroupId: string | null;
  description: string | null;
  category: string | null;
  /**
   * The run's advertised range, denormalised from the performances — the earliest start and the
   * latest end. Null when the event has no performances yet. Never edit these directly; they are
   * maintained by the server whenever a performance is added, moved or removed.
   */
  firstSessionStartsAt: string | null;
  lastSessionEndsAt: string | null;
  /** When the whole run goes on sale. An event-level decision — a run is advertised once. */
  onSaleAt: string | null;
  /** Counted across every performance of the run, not per night. */
  maxTicketsPerBuyer: number | null;
  requiresQueue: boolean;
  /** Sales-tax rate as a percentage (e.g. 18 for 18% GST); null when the event is untaxed. */
  taxRatePercent: number | null;
  /** Display name for the tax on a receipt (e.g. "GST 18%"). */
  taxLabel: string | null;
  /** Booking fee per ticket in minor units (e.g. 3000 for ₹30); 0 means no fee. */
  bookingFeePerTicketMinor: number;
  /** True only when *every* performance is paused. One paused night does not set this. */
  allSalesPaused: boolean;
  ageRestriction: string | null;
  bannerImageUrl: string | null;
  videoUrl: string | null;
  contactPhone: string | null;
  contactMobile: string | null;
  contactEmail: string | null;
  websiteUrl: string | null;
  socialLinks: SocialLinkResponse[];
  /** The performances of this event — what actually gets sold (ADR-0039). */
  sessions: EventSessionResponse[];
}

/** Lifecycle of one performance. Independent of the event's own status. */
export type EventSessionStatus = 'Draft' | 'Published' | 'Cancelled' | 'Completed';

/**
 * Which Venue block is sold as which ticket type, for this performance. Keyed by the block's
 * stable `code`, because a Venue seat carries no price and a rename must not break the mapping.
 * Per performance on purpose: Friday's Lower Tier can be Gold while Saturday's is Premium.
 */
export interface SessionAllocationResponse {
  code: string;
  ticketTypeId: string;
}

/**
 * One performance — a single night of an event. This is the grain everything downstream keys on:
 * inventory is provisioned per performance, an order and a ticket name one, and a scan is
 * validated against one.
 */
export interface EventSessionResponse {
  id: string;
  eventId: string;
  /** An optional label like "Matinee". Null when the date alone identifies it. */
  name: string | null;
  startsAt: string;
  endsAt: string;
  doorsOpenAt: string | null;
  /** "Book until two hours before this show" — a different instant every night. */
  bookingEndsAt: string | null;
  status: EventSessionStatus;
  /** Paused for this performance alone. Pulling one night does not pull the run. */
  salesPaused: boolean;
  venueId: string | null;
  seatMapId: string | null;
  /** The exact Venue version pinned at attach time; a later republish does not move it. */
  seatMapVersionId: string | null;
  seatMapVersionNumber: number | null;
  /** A display cache of the venue, never the source of truth — that is the Venue service. */
  venueName: string | null;
  city: string | null;
  country: string | null;
  timeZoneId: string | null;
  allocations: SessionAllocationResponse[];
}

/**
 * Fields settable via {@link updateSellingRules} — Draft-only (409 otherwise).
 *
 * What is left here after ADR-0039 is the money and the rules that apply to the **whole run**.
 * Dates and the venue moved to the performances that own them ({@link updateEventSession},
 * {@link attachSessionSeatMap}); title, description, imagery and contact details go through
 * {@link updateEventPresentation}, which works at any status.
 */
export interface UpdateSellingRulesRequest {
  requiresQueue: boolean;
  bookingFeePerTicketMinor: number;
  onSaleAt?: string | null;
  maxTicketsPerBuyer?: number | null;
  taxRatePercent?: number | null;
  taxLabel?: string | null;
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

/**
 * Fields for creating a new draft event, together with its **first performance**. An event with no
 * performance has nothing to sell, so the two are created together rather than leaving a window
 * where the event exists and is unsellable.
 *
 * There is no venue here: a performance names a Venue seat-map version, attached afterwards via
 * {@link attachSessionSeatMap} (ADR-0038/0039).
 */
export interface CreateEventRequest {
  title: string;
  currency: string;
  /** The first performance's times. */
  startsAt: string;
  endsAt: string;
  doorsOpenAt?: string | null;
  bookingEndsAt?: string | null;
  eventGroupId?: string | null;
  maxTicketsPerBuyer?: number | null;
  requiresQueue?: boolean;
  onSaleAt?: string | null;
  taxRatePercent?: number | null;
  taxLabel?: string | null;
  bookingFeePerTicketMinor?: number;
  /** Optional vanity URL slug; derived from the title when omitted. */
  slug?: string | null;
}

/** Creates a new draft event for the caller's tenant. */
export async function createEvent(request: CreateEventRequest): Promise<{ id: string }> {
  const response = await httpClient.post<{ id: string }>('/api/catalog/v1/events', request);
  return response.data;
}

/** Publishes a draft event, making it sellable. */
export async function publishEvent(eventId: string): Promise<void> {
  await httpClient.post(`/api/catalog/v1/events/${eventId}/publish`);
}

/**
 * Pauses sales across **every** performance of a published event, without affecting already-placed
 * holds or tickets. To pull a single night, use {@link pauseSessionSales}.
 */
export async function pauseSales(eventId: string): Promise<void> {
  await httpClient.post(`/api/catalog/v1/events/${eventId}/pause-sales`);
}

/** Resumes sales across every performance, undoing {@link pauseSales}. */
export async function resumeSales(eventId: string): Promise<void> {
  await httpClient.post(`/api/catalog/v1/events/${eventId}/resume-sales`);
}

/** Sets the run's selling rules — money, on-sale, buyer cap (Draft-only; 409 otherwise). */
export async function updateSellingRules(
  eventId: string,
  request: UpdateSellingRulesRequest,
): Promise<void> {
  await httpClient.put(`/api/catalog/v1/events/${eventId}/selling-rules`, request);
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

/** The times and label of one performance — everything about it that is not the venue or the map. */
export interface EventSessionRequest {
  startsAt: string;
  endsAt: string;
  name?: string | null;
  doorsOpenAt?: string | null;
  bookingEndsAt?: string | null;
}

/**
 * Lists an event's performances. Anonymous, like the event itself — a buyer choosing which night to
 * attend needs the list before they have done anything.
 */
export async function listEventSessions(eventId: string): Promise<EventSessionResponse[]> {
  const response = await httpClient.get<EventSessionResponse[]>(
    `/api/catalog/v1/events/${eventId}/sessions`,
  );
  return response.data;
}

/** Adds a performance to an event. Draft-only for the event's own performances. */
export async function addEventSession(
  eventId: string,
  request: EventSessionRequest,
): Promise<EventSessionResponse> {
  const response = await httpClient.post<EventSessionResponse>(
    `/api/catalog/v1/events/${eventId}/sessions`,
    request,
  );
  return response.data;
}

/** Reschedules or renames a performance. */
export async function updateEventSession(
  eventId: string,
  eventSessionId: string,
  request: EventSessionRequest,
): Promise<EventSessionResponse> {
  const response = await httpClient.put<EventSessionResponse>(
    `/api/catalog/v1/events/${eventId}/sessions/${eventSessionId}`,
    request,
  );
  return response.data;
}

/** Removes a performance. Refused (409) once it has been published — it may have sold tickets. */
export async function removeEventSession(eventId: string, eventSessionId: string): Promise<void> {
  await httpClient.delete(`/api/catalog/v1/events/${eventId}/sessions/${eventSessionId}`);
}

/**
 * Points a performance at a Venue seat map, pinning a version. Omit `versionNumber` to pin whatever
 * is published now — which is what you almost always want; naming one explicitly is for
 * deliberately selling against an older layout.
 *
 * Changing the pinned version clears the allocations, because the block codes may not survive.
 */
export async function attachSessionSeatMap(
  eventId: string,
  eventSessionId: string,
  request: { seatMapId: string; versionNumber?: number | null },
): Promise<EventSessionResponse> {
  const response = await httpClient.put<EventSessionResponse>(
    `/api/catalog/v1/events/${eventId}/sessions/${eventSessionId}/seat-map`,
    request,
  );
  return response.data;
}

/**
 * Sets which ticket type each block of the pinned seat map sells as. Replaces the whole map — one
 * row per Venue section or admission area code, about twenty for a stadium, not one per seat.
 *
 * A block left out is not spare capacity: publish refuses it, because Inventory would never hear
 * about those seats and the map would render with a hole nobody can tell from a sold-out block.
 */
export async function setSessionAllocations(
  eventId: string,
  eventSessionId: string,
  allocations: SessionAllocationResponse[],
): Promise<EventSessionResponse> {
  const response = await httpClient.put<EventSessionResponse>(
    `/api/catalog/v1/events/${eventId}/sessions/${eventSessionId}/allocations`,
    { allocations },
  );
  return response.data;
}

/**
 * Publishes one performance on its own — the late-show path, for a night added to an event that is
 * already selling. Publishing the event publishes all of them at once.
 */
export async function publishEventSession(
  eventId: string,
  eventSessionId: string,
): Promise<EventSessionResponse> {
  const response = await httpClient.post<EventSessionResponse>(
    `/api/catalog/v1/events/${eventId}/sessions/${eventSessionId}/publish`,
    {},
  );
  return response.data;
}

/** Cancels one performance without touching the rest of the run. */
export async function cancelEventSession(
  eventId: string,
  eventSessionId: string,
): Promise<EventSessionResponse> {
  const response = await httpClient.post<EventSessionResponse>(
    `/api/catalog/v1/events/${eventId}/sessions/${eventSessionId}/cancel`,
    {},
  );
  return response.data;
}

/** Pauses sales for one performance. Already-placed holds and issued tickets are untouched. */
export async function pauseSessionSales(
  eventId: string,
  eventSessionId: string,
): Promise<EventSessionResponse> {
  const response = await httpClient.post<EventSessionResponse>(
    `/api/catalog/v1/events/${eventId}/sessions/${eventSessionId}/pause-sales`,
    {},
  );
  return response.data;
}

/** Resumes sales for one paused performance. */
export async function resumeSessionSales(
  eventId: string,
  eventSessionId: string,
): Promise<EventSessionResponse> {
  const response = await httpClient.post<EventSessionResponse>(
    `/api/catalog/v1/events/${eventId}/sessions/${eventSessionId}/resume-sales`,
    {},
  );
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
  /** Ticket types the code is restricted to. Empty means every type in the order. */
  ticketTypeIds: string[];
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
  /** Ticket types the code is restricted to. Empty means every type in the order. */
  ticketTypeIds: string[];
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
  /** Omit or send [] to apply the code to every ticket type. */
  ticketTypeIds?: string[];
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
