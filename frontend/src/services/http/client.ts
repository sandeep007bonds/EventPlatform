import axios, { AxiosError } from 'axios';
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

    // No automatic toast here for ANY request, load or action — every call site (GET data loads
    // and POST/PUT/DELETE actions alike) already owns its own specific `.catch()` with either a
    // graceful in-place state (LoadError/NotFoundPage/ServerErrorPage) or a tailored toast message.
    // A generic toast here on top of that just doubled up on every single failure (see the git
    // history of this file for the exact bug report). 401 is the one status handled globally,
    // since logging the session out has to happen regardless of which call triggered it.
    if (error.response?.status === 401) {
      clearSession();
      window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT));
    }

    return Promise.reject(error);
  },
);
