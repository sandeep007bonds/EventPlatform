import { httpClient } from '../http/client';

/** A dev-login request. Stands in for real login until Identity/Entra exist (ADR-0015). */
export interface DevLoginRequest {
  email: string;
  tenantId?: string;
  role?: 'buyer' | 'organizer';
}

/** The resolved dev user returned alongside the issued token. */
export interface DevLoginUser {
  sub: string;
  email: string;
  tenantId: string;
  role: 'buyer' | 'organizer' | null;
}

/** Response body for a successful dev login. */
export interface DevLoginResponse {
  accessToken: string;
  expiresIn: number;
  tokenType: string;
  user: DevLoginUser;
}

/** Calls the gateway's Development-only dev-login endpoint. */
export async function devLogin(request: DevLoginRequest): Promise<DevLoginResponse> {
  const response = await httpClient.post<DevLoginResponse>('/api/auth/dev-login', request);
  return response.data;
}
