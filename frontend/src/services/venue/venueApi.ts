import { httpClient } from '../http/client';

/** Lifecycle of a venue. Only an `Active` venue should be offered when attaching a seat map. */
export type VenueStatus = 'Draft' | 'Active' | 'Archived';

/** Lifecycle of one seat-map version. Only a `Published` version can be sold against. */
export type SeatMapVersionStatus = 'Draft' | 'Published' | 'Superseded';

/** A venue's postal address. `latitude`/`longitude` are optional and only used for display. */
export interface VenueAddressResponse {
  addressLine1: string;
  addressLine2: string | null;
  city: string;
  region: string | null;
  postalCode: string | null;
  country: string;
  latitude: number | null;
  longitude: number | null;
}

/** An entry gate. A seat-map section or admission area may be restricted to one. */
export interface VenueGateResponse {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
}

/** A non-ticketed amenity — a bar, a restroom block, an accessible entrance. */
export interface VenueFacilityResponse {
  id: string;
  name: string;
  description: string | null;
}

/** One venue in full. */
export interface VenueResponse {
  id: string;
  tenantId: string;
  name: string;
  venueType: string | null;
  status: VenueStatus;
  timeZoneId: string | null;
  address: VenueAddressResponse;
  gates: VenueGateResponse[];
  facilities: VenueFacilityResponse[];
}

/** A venue as it appears in a list — enough to pick one, not enough to edit it. */
export interface VenueSummaryResponse {
  id: string;
  name: string;
  venueType: string | null;
  city: string;
  country: string;
  status: VenueStatus;
  gateCount: number;
}

/** The address fields a create/update accepts. */
export interface VenueAddressInput {
  addressLine1: string;
  addressLine2?: string | null;
  city: string;
  region?: string | null;
  postalCode?: string | null;
  country: string;
  latitude?: number | null;
  longitude?: number | null;
}

/** The body of a venue create or update — the two take the same shape. */
export interface VenueRequest {
  name: string;
  venueType?: string | null;
  address: VenueAddressInput;
  timeZoneId?: string | null;
}

/** Lists the tenant's venues. */
export async function listVenues(): Promise<VenueSummaryResponse[]> {
  const response = await httpClient.get<VenueSummaryResponse[]>('/api/venue/v1/venues');
  return response.data;
}

/** Fetches one venue in full, including its gates and facilities. */
export async function getVenue(venueId: string): Promise<VenueResponse> {
  const response = await httpClient.get<VenueResponse>(`/api/venue/v1/venues/${venueId}`);
  return response.data;
}

/** Creates a venue. It starts as `Draft` and must be activated before events can use it. */
export async function createVenue(request: VenueRequest): Promise<{ id: string }> {
  const response = await httpClient.post<{ id: string }>('/api/venue/v1/venues', request);
  return response.data;
}

/** Updates a venue's name, type, address and time zone. */
export async function updateVenue(venueId: string, request: VenueRequest): Promise<void> {
  await httpClient.put(`/api/venue/v1/venues/${venueId}`, request);
}

/** Activates a venue, making it selectable when attaching a seat map to a performance. */
export async function activateVenue(venueId: string): Promise<void> {
  await httpClient.post(`/api/venue/v1/venues/${venueId}/activate`, {});
}

/** Archives a venue. Existing performances keep the version they already pinned. */
export async function archiveVenue(venueId: string): Promise<void> {
  await httpClient.post(`/api/venue/v1/venues/${venueId}/archive`, {});
}

/** Adds an entry gate to a venue. */
export async function addVenueGate(
  venueId: string,
  request: { code: string; name: string },
): Promise<{ id: string }> {
  const response = await httpClient.post<{ id: string }>(
    `/api/venue/v1/venues/${venueId}/gates`,
    request,
  );
  return response.data;
}

/** Adds a facility to a venue. */
export async function addVenueFacility(
  venueId: string,
  request: { name: string; description?: string | null },
): Promise<{ id: string }> {
  const response = await httpClient.post<{ id: string }>(
    `/api/venue/v1/venues/${venueId}/facilities`,
    request,
  );
  return response.data;
}

/** One seat in a version's layout. `number` is a string because real venues use `12A`. */
export interface SeatResponse {
  id: string;
  number: string;
  attributes: string;
  isSellable: boolean;
}

/** One row of seats within a section. */
export interface SeatRowResponse {
  id: string;
  label: string;
  displayOrder: number;
  seats: SeatResponse[];
}

/**
 * One block of individually addressable seats. `code` is the stable identifier a performance's
 * allocation map references — it survives a rename, which `name` does not.
 */
export interface VenueSectionResponse {
  id: string;
  code: string;
  name: string;
  displayOrder: number;
  gateId: string | null;
  sellableSeatCount: number;
  rows: SeatRowResponse[];
  /**
   * What this block is normally sold as — "Lower Tier", "VIP", "GA" — or null when the venue has
   * no usual answer. A label, never a price (ADR-0041): it only saves re-typing the same mapping
   * for every event at the same venue, and nothing on the server reads it.
   */
  tierLabel: string | null;
}

/** One block sold by capacity rather than by seat. Also keyed by a stable `code`. */
export interface AdmissionAreaResponse {
  id: string;
  code: string;
  name: string;
  capacity: number;
  displayOrder: number;
  gateId: string | null;
  /**
   * What this block is normally sold as — "Lower Tier", "VIP", "GA" — or null when the venue has
   * no usual answer. A label, never a price (ADR-0041): it only saves re-typing the same mapping
   * for every event at the same venue, and nothing on the server reads it.
   */
  tierLabel: string | null;
}

/** A drawn shape on the map — purely graphical, and never what a ticket names. */
export interface SeatMapElementResponse {
  id: string;
  kind: string;
  shape: string;
  x: number;
  y: number;
  width: number;
  height: number;
  rotation: number;
  label: string | null;
  pointsJson: string | null;
  styleJson: string | null;
  sectionId: string | null;
  admissionAreaId: string | null;
}

/** One version of a seat map. Published versions are immutable — a change makes a new one. */
export interface SeatMapVersionResponse {
  id: string;
  versionNumber: number;
  status: SeatMapVersionStatus;
  publishedAt: string | null;
  capacity: number;
  sections: VenueSectionResponse[];
  admissionAreas: AdmissionAreaResponse[];
  elements: SeatMapElementResponse[];
}

/**
 * A seat map with **one** version resolved — the published one by default, or the one asked for by
 * `version`. The map itself carries no layout; every layout belongs to a version.
 */
export interface SeatMapResponse {
  id: string;
  venueId: string;
  tenantId: string;
  name: string;
  publishedVersionNumber: number | null;
  version: SeatMapVersionResponse;
}

/** A seat map as it appears in a list, without any layout. */
export interface SeatMapSummaryResponse {
  id: string;
  venueId: string;
  name: string;
  publishedVersionNumber: number | null;
  hasOpenDraft: boolean;
  versionCount: number;
}

/** Lists a venue's seat maps. */
export async function listSeatMaps(venueId: string): Promise<SeatMapSummaryResponse[]> {
  const response = await httpClient.get<SeatMapSummaryResponse[]>(
    `/api/venue/v1/venues/${venueId}/seat-maps`,
  );
  return response.data;
}

/** Creates a seat map on a venue, with an empty first draft. */
export async function createSeatMap(venueId: string, name: string): Promise<{ id: string }> {
  const response = await httpClient.post<{ id: string }>(
    `/api/venue/v1/venues/${venueId}/seat-maps`,
    { name },
  );
  return response.data;
}

/**
 * Fetches a seat map with one version's layout. Omit `versionNumber` for the published one — the
 * only version a buyer or a publish check should ever be shown.
 */
export async function getSeatMap(
  seatMapId: string,
  versionNumber?: number,
): Promise<SeatMapResponse> {
  const response = await httpClient.get<SeatMapResponse>(
    `/api/venue/v1/seat-maps/${seatMapId}`,
    versionNumber == null ? undefined : { params: { version: versionNumber } },
  );
  return response.data;
}

/**
 * Opens a new draft version, pre-filled from the published layout with fresh ids. Fails (409) if a
 * draft is already open — there is only ever one.
 */
export async function startSeatMapDraft(seatMapId: string): Promise<SeatMapResponse> {
  const response = await httpClient.post<SeatMapResponse>(
    `/api/venue/v1/seat-maps/${seatMapId}/versions`,
    {},
  );
  return response.data;
}

/** One seat in a layout being saved. */
export interface SeatMapSeatInput {
  number: string;
  attributes?: string[];
  isSellable?: boolean;
}

/** One row in a layout being saved. */
export interface SeatMapRowInput {
  label: string;
  displayOrder: number;
  seats?: SeatMapSeatInput[];
}

/** One reserved-seat block in a layout being saved. */
export interface SeatMapSectionInput {
  code: string;
  name: string;
  displayOrder: number;
  gateId?: string | null;
  rows?: SeatMapRowInput[];
  /** What this block is normally sold as, or null/omitted. A label, never a price (ADR-0041). */
  tierLabel?: string | null;
}

/** One capacity-only block in a layout being saved. */
export interface SeatMapAdmissionAreaInput {
  code: string;
  name: string;
  capacity: number;
  displayOrder: number;
  gateId?: string | null;
  /** What this block is normally sold as, or null/omitted. A label, never a price (ADR-0041). */
  tierLabel?: string | null;
}

/** One drawn shape in a layout being saved, bound to a block by its code rather than its id. */
export interface SeatMapElementInput {
  kind: string;
  shape: string;
  x: number;
  y: number;
  width: number;
  height: number;
  rotation?: number;
  label?: string | null;
  pointsJson?: string | null;
  styleJson?: string | null;
  sectionCode?: string | null;
  admissionAreaCode?: string | null;
}

/**
 * Replaces the open draft's whole layout. Whole-layout replacement is the only write: a partial
 * edit would have to reconcile ids the caller does not own, and the draft is cheap to resend.
 */
export async function saveSeatMapLayout(
  seatMapId: string,
  layout: {
    sections?: SeatMapSectionInput[];
    admissionAreas?: SeatMapAdmissionAreaInput[];
    elements?: SeatMapElementInput[];
  },
): Promise<SeatMapResponse> {
  const response = await httpClient.put<SeatMapResponse>(
    `/api/venue/v1/seat-maps/${seatMapId}/draft/layout`,
    layout,
  );
  return response.data;
}

/** A single reason a draft could not be published. */
export interface SeatMapValidationError {
  code: string;
  message: string;
}

/**
 * Publishes the open draft, superseding whatever was published before. A 409 carries **every**
 * validation failure, not the first — an organizer fixing a map needs the whole list at once.
 */
export async function publishSeatMap(
  seatMapId: string,
): Promise<{ versionNumber: number; capacity: number }> {
  const response = await httpClient.post<{ versionNumber: number; capacity: number }>(
    `/api/venue/v1/seat-maps/${seatMapId}/publish`,
    {},
  );
  return response.data;
}
