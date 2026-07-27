import axios, { AxiosError } from 'axios';
import { toast } from '../../components/common/feedback/toast';
import { clearSession, getSession, SESSION_EXPIRED_EVENT } from './tokenStore';

// The frontend only ever talks to the gateway (BFF) — never a backend service
// directly. See frontend/CLAUDE.md.
const baseURL = import.meta.env.VITE_GATEWAY_BASE_URL;

export const httpClient = axios.create({ baseURL });

httpClient.interceptors.request.use((config) => {
  const session = getSession();
  if (session) {
    config.headers.set('Authorization', `Bearer ${session.accessToken}`);
  }

  return config;
});

httpClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    const status = error.response?.status;

    if (status === 401) {
      clearSession();
      window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT));
    } else if (status === 403) {
      toast.error('You do not have permission to do that.');
    } else if (status === undefined || status >= 500) {
      toast.error('Something went wrong. Please try again.');
    }

    return Promise.reject(error);
  },
);
