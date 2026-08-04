import { useCallback, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { verifyOtp } from '../services/auth/identityApi';
import {
  loginOrganizer,
  registerOrganizer as registerOrganizerRequest,
} from '../services/auth/organizerApi';
import {
  clearSession,
  getSession,
  setSession,
  SESSION_EXPIRED_EVENT,
  type StoredUser,
} from '../services/http/tokenStore';
import { AuthContext } from './authContextValue';

/** Provides the current auth state to the whole app. Wrap once, near the root. */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<StoredUser | null>(() => getSession()?.user ?? null);

  useEffect(() => {
    const onSessionExpired = () => setUser(null);
    window.addEventListener(SESSION_EXPIRED_EVENT, onSessionExpired);
    return () => window.removeEventListener(SESSION_EXPIRED_EVENT, onSessionExpired);
  }, []);

  const loginWithOtp = useCallback(async (phoneNumber: string, code: string) => {
    const response = await verifyOtp({ phoneNumber, code });
    const otpUser: StoredUser = { sub: response.buyerId, role: 'buyer' };

    // response.expiresAt is already an absolute ISO timestamp — unlike dev-login's relative
    // expiresIn, no Date.now() arithmetic is needed here.
    setSession({
      accessToken: response.accessToken,
      expiresAtIso: response.expiresAt,
      user: otpUser,
    });
    setUser(otpUser);
  }, []);

  const registerOrganizer = useCallback(
    async (organizationName: string, email: string, password: string) => {
      const response = await registerOrganizerRequest({ organizationName, email, password });
      const organizerUser: StoredUser = {
        sub: response.organizerId,
        email,
        tenantId: response.tenantId,
        role: 'organizer',
      };

      setSession({
        accessToken: response.accessToken,
        expiresAtIso: response.expiresAt,
        user: organizerUser,
      });
      setUser(organizerUser);
    },
    [],
  );

  const loginWithOrganizerCredentials = useCallback(async (email: string, password: string) => {
    const response = await loginOrganizer({ email, password });
    const organizerUser: StoredUser = {
      sub: response.organizerId,
      email,
      tenantId: response.tenantId,
      role: 'organizer',
    };

    setSession({
      accessToken: response.accessToken,
      expiresAtIso: response.expiresAt,
      user: organizerUser,
    });
    setUser(organizerUser);
  }, []);

  const logout = useCallback(() => {
    clearSession();
    setUser(null);
  }, []);

  const value = useMemo(
    () => ({ user, loginWithOtp, registerOrganizer, loginWithOrganizerCredentials, logout }),
    [user, loginWithOtp, registerOrganizer, loginWithOrganizerCredentials, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
