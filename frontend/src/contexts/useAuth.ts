import { useContext } from 'react';
import { AuthContext, type AuthContextValue } from './authContextValue';

/** Reads the current auth state. Must be called under `AuthProvider`. */
export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }

  return context;
}
