// sessionStorage-backed helpers for the Queue waiting room. A session id survives a page refresh
// (so rejoining resumes the same position instead of going to the back of the line); an admission
// token's expiry is decoded client-side from its own payload (no network round trip needed just to
// know it's stale) — see HmacAdmissionTokenIssuer in Queue.Infrastructure for the token format.

const sessionIdKey = (eventId: string) => `queue:${eventId}:sessionId`;
const admissionTokenKey = (eventId: string) => `queue:${eventId}:admissionToken`;

/** Returns this tab's queue session id for an event, minting one on first use. */
export function getOrCreateQueueSessionId(eventId: string): string {
  const key = sessionIdKey(eventId);
  const existing = sessionStorage.getItem(key);
  if (existing) {
    return existing;
  }
  const created = crypto.randomUUID();
  sessionStorage.setItem(key, created);
  return created;
}

/** Stashes an admission token once a queue session is admitted. */
export function storeAdmissionToken(eventId: string, token: string): void {
  sessionStorage.setItem(admissionTokenKey(eventId), token);
}

/** Returns a still-valid stashed admission token for the event, or null — clearing a stale one. */
export function getValidAdmissionToken(eventId: string): string | null {
  const token = sessionStorage.getItem(admissionTokenKey(eventId));
  if (!token) {
    return null;
  }

  const parts = token.split('.');
  const expSeconds = parts.length === 4 ? Number(parts[2]) : NaN;
  if (!Number.isFinite(expSeconds) || expSeconds * 1000 <= Date.now()) {
    sessionStorage.removeItem(admissionTokenKey(eventId));
    return null;
  }

  return token;
}
