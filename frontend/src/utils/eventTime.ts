/**
 * Formatting event dates in the venue's time zone rather than the reader's.
 *
 * Every date the API returns is an instant (ISO 8601 with an offset), so *when* an event happens
 * is never ambiguous. What is ambiguous without this is *what to print*: `dayjs(startsAt).format()`
 * renders in whatever zone the reader's browser is in, so a 7pm Delhi show reads as 1:30pm to a
 * buyer in London and 9:30am to one in New York. For a door time that is not a rounding error, it
 * is the wrong answer.
 *
 * `Intl.DateTimeFormat` does the work — it ships with the browser's own tz database, so there is no
 * dependency here and no dayjs timezone plugin to keep in step.
 */

/** Formats an ISO instant in `timeZoneId`, falling back to the reader's own zone when unknown. */
export function formatEventDateTime(iso: string, timeZoneId?: string | null): string {
  return format(iso, timeZoneId, {
    weekday: undefined,
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
}

/** As {@link formatEventDateTime}, with the weekday — for a single event's headline date. */
export function formatEventDateTimeLong(iso: string, timeZoneId?: string | null): string {
  return format(iso, timeZoneId, {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
}

/** Date only, no time — for list rows and tour legs. */
export function formatEventDate(iso: string, timeZoneId?: string | null): string {
  return format(iso, timeZoneId, { day: 'numeric', month: 'short', year: 'numeric' });
}

/** Time only — for a doors-open time shown next to a start time already carrying the date. */
export function formatEventTime(iso: string, timeZoneId?: string | null): string {
  return format(iso, timeZoneId, { hour: 'numeric', minute: '2-digit' });
}

/**
 * The zone's short name at that instant (e.g. `GMT+5:30`, `PDT`), or `null` when the event has no
 * zone set.
 *
 * Worth appending wherever a reader might be elsewhere: "7:00 PM" is only unambiguous once it says
 * which 7pm. Resolved at the given instant rather than statically, so an event on the far side of a
 * DST change is labelled with the offset that will actually be in force.
 */
export function eventZoneAbbreviation(iso: string, timeZoneId?: string | null): string | null {
  if (!timeZoneId) {
    return null;
  }
  try {
    const parts = new Intl.DateTimeFormat(undefined, {
      timeZone: timeZoneId,
      timeZoneName: 'short',
    }).formatToParts(new Date(iso));
    return parts.find((part) => part.type === 'timeZoneName')?.value ?? null;
  } catch {
    return null;
  }
}

function format(
  iso: string,
  timeZoneId: string | null | undefined,
  options: Intl.DateTimeFormatOptions,
): string {
  const date = new Date(iso);
  try {
    return new Intl.DateTimeFormat(undefined, {
      ...options,
      ...(timeZoneId ? { timeZone: timeZoneId } : {}),
    }).format(date);
  } catch {
    // An id the browser's tz database doesn't know — stale data, or a zone added since this
    // browser shipped. Falling back to the reader's zone shows a defensible time rather than
    // breaking the page over a formatting concern.
    return new Intl.DateTimeFormat(undefined, options).format(date);
  }
}
