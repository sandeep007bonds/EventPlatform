import type { EventResponse, EventSessionResponse } from '../services/catalog/catalogApi';
import { formatEventDateTime } from './eventTime';

/**
 * Helpers for the shape an event took on in ADR-0039: a run of one or more **performances**, where
 * the performance — not the event — is what has a time, a venue and something to sell.
 *
 * Most screens used to read `event.startsAt` and `event.city`. Those fields are gone, and the
 * honest replacement is "which performance are we talking about?" — which is a real question with
 * more than one answer, so it lives here rather than being guessed at in eight components.
 */

/** A performance a buyer can actually act on right now. */
export function isSellable(session: EventSessionResponse): boolean {
  return session.status === 'Published' && !session.salesPaused;
}

/** Performances in the order a person reads them: earliest first. */
export function inStartOrder(sessions: EventSessionResponse[]): EventSessionResponse[] {
  return [...sessions].sort((a, b) => a.startsAt.localeCompare(b.startsAt));
}

/**
 * The performance a summary should speak for — the next one still to come, or, once the whole run
 * is past, the last one. Never simply "the first in the array": a tour's closing night is not what
 * a listing should advertise in the middle of the run.
 */
export function primarySession(event: EventResponse): EventSessionResponse | null {
  const ordered = inStartOrder(event.sessions);
  if (ordered.length === 0) {
    return null;
  }

  const now = new Date().toISOString();
  return ordered.find((session) => session.endsAt >= now) ?? ordered[ordered.length - 1];
}

/** The performances still worth showing a buyer — upcoming and on sale, earliest first. */
export function upcomingSellableSessions(event: EventResponse): EventSessionResponse[] {
  const now = new Date().toISOString();
  return inStartOrder(event.sessions).filter(
    (session) => isSellable(session) && session.endsAt >= now,
  );
}

/**
 * How to name one performance in a list. The organizer's own label wins when they gave one
 * ("Matinee"), because they chose it precisely to distinguish two shows on the same day; otherwise
 * the date and time do the work, rendered in the **venue's** zone (see `eventTime.ts`).
 */
export function sessionLabel(session: EventSessionResponse): string {
  const when = formatEventDateTime(session.startsAt, session.timeZoneId);
  return session.name ? `${session.name} · ${when}` : when;
}

/** "Royal Albert Hall, London", or null when no seat map has been attached yet. */
export function venueLabel(session: EventSessionResponse | null): string | null {
  if (session?.venueName == null) {
    return null;
  }

  return session.city ? `${session.venueName}, ${session.city}` : session.venueName;
}

/**
 * The run's advertised span, as one string. Uses the server's denormalised range rather than
 * recomputing from `sessions`, which a list response may have truncated.
 */
export function runLabel(event: EventResponse): string | null {
  if (event.firstSessionStartsAt == null) {
    return null;
  }

  const zone = primarySession(event)?.timeZoneId ?? null;
  const first = formatEventDateTime(event.firstSessionStartsAt, zone);
  if (event.sessions.length <= 1 || event.lastSessionEndsAt == null) {
    return first;
  }

  return `${first} — ${formatEventDateTime(event.lastSessionEndsAt, zone)}`;
}

/** Looks one performance up by id, for a page that has the event and an `:eventSessionId` param. */
export function findSession(
  event: EventResponse,
  eventSessionId: string | undefined,
): EventSessionResponse | null {
  if (eventSessionId == null) {
    return null;
  }

  return event.sessions.find((session) => session.id === eventSessionId) ?? null;
}
