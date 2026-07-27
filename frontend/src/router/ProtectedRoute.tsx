import type { ReactNode } from 'react';
import { useAuth } from '../contexts/useAuth';
import { UnauthorizedPage } from '../components/common/errors/UnauthorizedPage';

/**
 * Gates a route behind login. Not a role check — `role` is a UI-routing convenience
 * only (see `DevLoginRequest.Role` on the gateway), never enforced by any backend
 * authorization today. Buyer/admin separation here is "which section are you in,"
 * not a security boundary.
 */
export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { user } = useAuth();

  if (!user) {
    return <UnauthorizedPage />;
  }

  return <>{children}</>;
}
