import { createContext } from 'react';
import type { StoredUser } from '../services/http/tokenStore';

export interface AuthContextValue {
  /** The logged-in user, or `null` if nobody is logged in. */
  user: StoredUser | null;
  /** Verifies a buyer's OTP code with Identity and mints their session on success (ADR-0016). */
  loginWithOtp: (phoneNumber: string, code: string) => Promise<void>;
  /** Registers a new organization + its first organizer account and mints their session (ADR-0023). */
  registerOrganizer: (organizationName: string, email: string, password: string) => Promise<void>;
  /** Logs in with an existing organizer email+password and mints their session (ADR-0023). */
  loginWithOrganizerCredentials: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
