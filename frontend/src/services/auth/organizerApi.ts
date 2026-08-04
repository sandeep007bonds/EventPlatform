import { httpClient } from '../http/client';

/** A request to register a new organization and its first organizer account. */
export interface RegisterOrganizerRequest {
  organizationName: string;
  email: string;
  password: string;
}

/** A request to log in with an existing organizer email+password. */
export interface LoginOrganizerRequest {
  email: string;
  password: string;
}

/** Response body for a successful organizer registration or login. */
export interface OrganizerAuthResponse {
  accessToken: string;
  tokenType: string;
  expiresAt: string;
  organizerId: string;
  tenantId: string;
}

/** Shape of the error body Identity returns on 400/401/409/423 for the organizer endpoints. */
export interface OrganizerAuthErrorBody {
  error: string;
}

/** Registers a new organization and its first organizer account. */
export async function registerOrganizer(
  request: RegisterOrganizerRequest,
): Promise<OrganizerAuthResponse> {
  const response = await httpClient.post<OrganizerAuthResponse>(
    '/api/identity/v1/organizers/register',
    request,
  );
  return response.data;
}

/** Logs in with an existing organizer email+password. */
export async function loginOrganizer(
  request: LoginOrganizerRequest,
): Promise<OrganizerAuthResponse> {
  const response = await httpClient.post<OrganizerAuthResponse>(
    '/api/identity/v1/organizers/login',
    request,
  );
  return response.data;
}
