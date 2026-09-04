import type { VenueRequest, VenueResponse } from '../../../services/venue/venueApi';

/** What the venue form collects. Flat, because a form is flat; nested into an address on submit. */
export interface VenueFormValues {
  name: string;
  venueType?: string;
  timeZoneId?: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  region?: string;
  postalCode?: string;
  country: string;
  latitude?: number;
  longitude?: number;
}

/**
 * Every IANA zone the browser knows, straight from the platform's own tz database — so the list
 * cannot go stale against a bundled copy, and the zones offered are exactly the ones
 * `formatEventDateTime` can render. `supportedValuesOf` is not in every engine; falling back to a
 * short list of common zones keeps the field usable rather than empty.
 */
export const TIME_ZONE_OPTIONS = (
  typeof Intl.supportedValuesOf === 'function'
    ? Intl.supportedValuesOf('timeZone')
    : ['Asia/Kolkata', 'Asia/Qatar', 'Asia/Dubai', 'Europe/London', 'America/New_York', 'UTC']
).map((zone) => ({ value: zone, label: zone }));

/** Turns the flat form values into the nested shape the Venue API takes. */
export function toVenueRequest(values: VenueFormValues): VenueRequest {
  return {
    name: values.name.trim(),
    venueType: values.venueType?.trim() || null,
    timeZoneId: values.timeZoneId || null,
    address: {
      addressLine1: values.addressLine1.trim(),
      addressLine2: values.addressLine2?.trim() || null,
      city: values.city.trim(),
      region: values.region?.trim() || null,
      postalCode: values.postalCode?.trim() || null,
      country: values.country.trim().toUpperCase(),
      latitude: values.latitude ?? null,
      longitude: values.longitude ?? null,
    },
  };
}

/** Flattens a fetched venue back into the form's shape, for editing. */
export function toFormValues(venue: VenueResponse): VenueFormValues {
  return {
    name: venue.name,
    venueType: venue.venueType ?? undefined,
    timeZoneId: venue.timeZoneId ?? undefined,
    addressLine1: venue.address.addressLine1,
    addressLine2: venue.address.addressLine2 ?? undefined,
    city: venue.address.city,
    region: venue.address.region ?? undefined,
    postalCode: venue.address.postalCode ?? undefined,
    country: venue.address.country,
    latitude: venue.address.latitude ?? undefined,
    longitude: venue.address.longitude ?? undefined,
  };
}
