import type { Dayjs } from 'dayjs';

/**
 * Form-value shape for one leg (city/date) in `CreateEventPage`'s repeater — mirrors
 * `CreateEventRequest` minus `eventGroupId`, which is set once for the whole batch, not per leg.
 */
export interface EventLegFormValues {
  title: string;
  startsAt: Dayjs;
  endsAt: Dayjs;
  currency: string;
  locationName: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  region?: string;
  postalCode?: string;
  country: string;
  latitude?: number;
  longitude?: number;
  maxTicketsPerBuyer?: number;
  requiresQueue?: boolean;
}

/** Status of one leg's submission within the current page visit. */
export type LegStatus = 'pending' | 'created' | 'failed';

/**
 * A blank leg, used as the form's initial single leg and seeded into each newly-added one — split
 * into its own file (not exported alongside a component) purely so react-refresh's "a file should
 * only export components" lint rule stays happy, matching `seatMapSectionDefaults.ts`'s precedent.
 */
export const DEFAULT_LEG: Partial<EventLegFormValues> = {
  currency: 'USD',
};
