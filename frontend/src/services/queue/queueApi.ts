import { httpClient } from '../http/client';

/** Response shared by {@link joinQueue} and {@link getQueueStatus}. */
export interface QueueSessionResponse {
  sessionId: string;
  admitted: boolean;
  admissionToken: string | null;
  position: number | null;
  estimatedWaitSeconds: number | null;
}

/** Joins (or resumes) an event's waiting room. Anonymous — no login required. */
export async function joinQueue(
  eventId: string,
  sessionId?: string,
): Promise<QueueSessionResponse> {
  const response = await httpClient.post<QueueSessionResponse>(
    `/api/queue/v1/events/${eventId}/queue/join`,
    { sessionId },
  );
  return response.data;
}

/** Polls a session's current waiting-room status. Anonymous — no login required. */
export async function getQueueStatus(
  eventId: string,
  sessionId: string,
): Promise<QueueSessionResponse> {
  const response = await httpClient.get<QueueSessionResponse>(
    `/api/queue/v1/events/${eventId}/queue/status`,
    { params: { sessionId } },
  );
  return response.data;
}

/** An event's waiting-room pacing configuration. */
export interface QueueSettingsResponse {
  eventId: string;
  enabled: boolean;
  admissionRatePerInterval: number;
  intervalSeconds: number;
  sessionTtlSeconds: number;
}

/** Fields tunable via {@link updateQueueSettings} — `enabled` is fixed at provisioning time, not editable here. */
export interface UpdateQueueSettingsRequest {
  admissionRatePerInterval: number;
  intervalSeconds: number;
  sessionTtlSeconds: number;
}

/** Fetches an event's queue pacing configuration (tenant-owned). 404s if not yet provisioned. */
export async function getQueueSettings(eventId: string): Promise<QueueSettingsResponse> {
  const response = await httpClient.get<QueueSettingsResponse>(
    `/api/queue/v1/events/${eventId}/queue/settings`,
  );
  return response.data;
}

/** Tunes an event's admission pacing (tenant-owned). */
export async function updateQueueSettings(
  eventId: string,
  request: UpdateQueueSettingsRequest,
): Promise<void> {
  await httpClient.put(`/api/queue/v1/events/${eventId}/queue/settings`, request);
}
