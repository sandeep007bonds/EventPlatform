// Mints dev JWTs (HS256) that the services accept when `Jwt:DevSigningKey` is set (Development
// only). No identity provider required — purely for local testing / load testing.
import crypto from 'k6/crypto';
import encoding from 'k6/encoding';

function b64url(obj) {
  return encoding.b64encode(JSON.stringify(obj), 'rawurl');
}

// claims must include tenant_id and sub (both GUIDs) — the services read those.
export function mintToken(secret, claims) {
  const header = { alg: 'HS256', typ: 'JWT' };
  const now = Math.floor(Date.now() / 1000);
  const payload = Object.assign(
    { iss: 'eventplatform-dev', aud: 'eventplatform', iat: now, exp: now + 3600 },
    claims,
  );
  const signingInput = `${b64url(header)}.${b64url(payload)}`;
  const signature = crypto.hmac('sha256', secret, signingInput, 'base64rawurl');
  return `${signingInput}.${signature}`;
}

export function uuidv4() {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}
