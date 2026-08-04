import { useCallback, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { devLogin } from '../services/auth/authApi';
import { verifyOtp } from '../services/auth/identityApi';
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

  const loginWithDevCredentials = useCallback(
    async (email: string, role: 'buyer' | 'organizer') => {
      const response = await devLogin({ email, role });
      const expiresAtIso = new Date(Date.now() + response.expiresIn * 1000).toISOString();

      setSession({ accessToken: response.accessToken, expiresAtIso, user: response.user });
      setUser(response.user);
    },
    [],
  );

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

  const logout = useCallback(() => {
    clearSession();
    setUser(null);
  }, []);

  const value = useMemo(
    () => ({ user, loginWithDevCredentials, loginWithOtp, logout }),
    [user, loginWithDevCredentials, loginWithOtp, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
