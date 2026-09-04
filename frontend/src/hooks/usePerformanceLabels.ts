import { useEffect, useState } from 'react';
import { getEvent } from '../services/catalog/catalogApi';
import { sessionLabel } from '../utils/eventSessions';

/** How one performance reads in a list: the event's title, and which night it is. */
export interface PerformanceLabel {
  eventTitle: string;
  performance: string;
}

/**
 * Resolves `{ catalogEventId, eventSessionId }` pairs into something a person can read.
 *
 * Orders and tickets carry ids, not names — that is right for the wire and useless in a table. Once
 * an event runs several nights, "which night is this order for" stops being a nicety: a support
 * agent looking at a refund request, or a buyer scanning their own history, cannot tell two orders
 * apart without it.
 *
 * Fetches **one event per distinct event id on the page**, not one per row, and caches across
 * renders so paging back and forth costs nothing. Failures are silent by design: a missing label
 * degrades a row to its ids, which is worse than a name but far better than an error state on a
 * page whose actual subject loaded fine.
 */
export function usePerformanceLabels(
  rows: { catalogEventId: string; eventSessionId: string }[],
): Map<string, PerformanceLabel> {
  const [labels, setLabels] = useState<Map<string, PerformanceLabel>>(new Map());

  // A stable key for "which events does this page need", so the effect re-runs when the page
  // changes but not on every render that happens to produce a new array instance.
  const eventIds = [...new Set(rows.map((row) => row.catalogEventId))].sort().join(',');

  useEffect(() => {
    if (eventIds === '') {
      return;
    }

    let cancelled = false;

    void Promise.all(
      eventIds.split(',').map((eventId) => getEvent(eventId).catch(() => null)),
    ).then((events) => {
      if (cancelled) {
        return;
      }

      setLabels((previous) => {
        const next = new Map(previous);
        for (const event of events) {
          if (!event) {
            continue;
          }
          for (const session of event.sessions) {
            next.set(session.id, {
              eventTitle: event.title,
              performance: sessionLabel(session),
            });
          }
        }
        return next;
      });
    });

    return () => {
      cancelled = true;
    };
  }, [eventIds]);

  return labels;
}
