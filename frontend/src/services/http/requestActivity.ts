type Listener = (activeCount: number) => void;

// Tracks how many HTTP requests are currently in flight so `TopProgressBar` can show/hide
// itself without needing React context — same plain-module pattern as `toast.ts`, so the
// axios interceptors (outside React) can drive it directly.
let activeCount = 0;
const listeners = new Set<Listener>();

/** Called by the axios request interceptor when a request is sent. */
export function beginRequest(): void {
  activeCount += 1;
  notify();
}

/** Called by the axios response interceptor once a request settles (success or error). */
export function endRequest(): void {
  activeCount = Math.max(0, activeCount - 1);
  notify();
}

/** Subscribes to active-request-count changes. Returns an unsubscribe function. */
export function subscribeRequestActivity(listener: Listener): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

function notify(): void {
  for (const listener of listeners) {
    listener(activeCount);
  }
}
