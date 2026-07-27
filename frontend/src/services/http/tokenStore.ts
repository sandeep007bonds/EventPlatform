// Plain module, not a hook: the axios interceptors below run outside React's render
// cycle and need synchronous, non-hook access to the current token.
//
// sessionStorage, not localStorage: this is an explicit phase-1 choice (see
// frontend/CLAUDE.md and ADR-0015) that scopes an XSS token leak to the tab's
// lifetime. The hardened target state — an httpOnly/Secure/SameSite cookie
// issued and read only by the gateway, plus CSRF protection — is required
// before a real production rollout, not before.
const STORAGE_KEY = 'eventplatform.token';

/** The resolved user a stored session was issued for. */
export interface StoredUser {
  sub: string;
  email: string;
  tenantId: string;
  role: 'buyer' | 'organizer' | null;
}

/** A stored bearer token plus the resolved user it was issued for. */
export interface StoredSession {
  accessToken: string;
  expiresAtIso: string;
  user: StoredUser;
}

let cached: StoredSession | null = readFromStorage();

function readFromStorage(): StoredSession | null {
  const raw = sessionStorage.getItem(STORAGE_KEY);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as StoredSession;
  } catch {
    sessionStorage.removeItem(STORAGE_KEY);
    return null;
  }
}

/** Reads the current session, or `null` if nobody is logged in. */
export function getSession(): StoredSession | null {
  return cached;
}

/** Persists a newly issued session (overwrites any previous one). */
export function setSession(session: StoredSession): void {
  cached = session;
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
}

/** Clears the current session (logout, or a 401 response). */
export function clearSession(): void {
  cached = null;
  sessionStorage.removeItem(STORAGE_KEY);
}

/** Dispatched on `window` whenever a response interceptor clears the session on a 401. */
export const SESSION_EXPIRED_EVENT = 'eventplatform:session-expired';
