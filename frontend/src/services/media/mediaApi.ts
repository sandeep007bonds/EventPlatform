import { httpClient } from '../http/client';

/** Response from a successful image upload. */
export interface UploadImageResponse {
  url: string;
}

/**
 * Uploads an image and returns its public URL. Two separate calls from the caller's
 * perspective — this only uploads the file; saving the returned URL onto an event is a
 * separate `updateEventPresentation` call against Catalog. Uses the shared `httpClient` (per
 * frontend/CLAUDE.md's "one axios instance" rule) — no Content-Type header is set here, so
 * axios/the browser attach the correct `multipart/form-data` boundary automatically.
 */
export async function uploadImage(file: File): Promise<UploadImageResponse> {
  const formData = new FormData();
  formData.append('file', file);

  const response = await httpClient.post<UploadImageResponse>(
    '/api/media/v1/media/images',
    formData,
  );
  return response.data;
}
