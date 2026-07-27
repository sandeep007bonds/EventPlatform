import { createContext } from 'react';
import type { StoredUser } from '../services/http/tokenStore';

export interface AuthContextValue {
  /** The logged-in user, or `null` if nobody is logged in. */
  user: StoredUser | null;
  /** Logs in via the gateway's dev-login endpoint (stands in for real auth — see ADR-0015). */
  loginWithDevCredentials: (email: string, role: 'buyer' | 'organizer') => Promise<void>;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
