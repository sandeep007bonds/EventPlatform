import { httpClient } from '../http/client';

/** A request to send a one-time code to a buyer's phone. */
export interface RequestOtpRequest {
  phoneNumber: string;
}

/** Response body when an OTP has been sent. */
export interface RequestOtpResponse {
  expiresInSeconds: number;
}

/** A request to verify a previously-sent one-time code. */
export interface VerifyOtpRequest {
  phoneNumber: string;
  code: string;
}

/** Response body for a successful OTP verification — mints the buyer's session. */
export interface VerifyOtpResponse {
  accessToken: string;
  tokenType: string;
  expiresAt: string;
  buyerId: string;
}

/** Shape of the error body Identity returns on 400/423/429 for the OTP endpoints. */
export interface OtpErrorBody {
  error: string;
  retryAfterSeconds?: number;
}

/** Requests a one-time code be sent to the given phone number. */
export async function requestOtp(request: RequestOtpRequest): Promise<RequestOtpResponse> {
  const response = await httpClient.post<RequestOtpResponse>(
    '/api/identity/v1/otp/request',
    request,
  );
  return response.data;
}

/** Verifies a one-time code and mints an access token for the buyer. */
export async function verifyOtp(request: VerifyOtpRequest): Promise<VerifyOtpResponse> {
  const response = await httpClient.post<VerifyOtpResponse>('/api/identity/v1/otp/verify', request);
  return response.data;
}
