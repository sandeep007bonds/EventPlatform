import axios, { AxiosError } from 'axios';
import { toast } from '../../components/common/feedback/toast';
import { beginRequest, endRequest } from './requestActivity';
import { clearSession, getSession, SESSION_EXPIRED_EVENT } from './tokenStore';

// The frontend only ever talks to the gateway (BFF) — never a backend service
// directly. See frontend/CLAUDE.md.
const baseURL = import.meta.env.VITE_GATEWAY_BASE_URL;

export const httpClient = axios.create({ baseURL });

httpClient.interceptors.request.use((config) => {
  beginRequest();

  const session = getSession();
  if (session) {
    config.headers.set('Authorization', `Bearer ${session.accessToken}`);
  }

  return config;
});

httpClient.interceptors.response.use(
  (response) => {
    endRequest();
    return response;
  },
  (error: AxiosError) => {
    endRequest();

    const status = error.response?.status;
    // GET failures are page/panel data loads — the caller owns showing a graceful in-place state
    // (LoadError, NotFoundPage, ServerErrorPage, an Empty view) for those, not a toast; a toast
    // here on top of that would just double up (see the caller's own .catch(), which every load
    // site has). Mutating requests (POST/PUT/DELETE) are user-triggered actions — most already
    // show their own specific toast, but this stays as a fallback so an action with no bespoke
    // handler still gives the user *some* feedback instead of failing silently.
    const isLoad = error.config?.method?.toLowerCase() === 'get';

    if (status === 401) {
      clearSession();
      window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT));
    } else if (!isLoad && status === 403) {
      toast.error('You do not have permission to do that.');
    } else if (!isLoad && (status === undefined || status >= 500)) {
      toast.error('Something went wrong. Please try again.');
    }

    return Promise.reject(error);
  },
);
