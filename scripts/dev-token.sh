#!/usr/bin/env bash
# Mints a dev HS256 JWT accepted by every service's Development-only auth path
# (Jwt:DevSigningKey in appsettings.Development.json — never used in production).
# Prints just the token to stdout, so it composes with `export TOKEN=$(...)`.
#
# Usage:
#   export TOKEN=$(./scripts/dev-token.sh)
#   export TOKEN=$(./scripts/dev-token.sh <user-guid>)   # override the buyer (sub claim)
set -euo pipefail

if ! command -v python3 >/dev/null 2>&1; then
  echo "python3 is required to mint a token (used only for the HMAC signature). Install it, or mint a token by hand at https://jwt.io." >&2
  exit 1
fi

SUB="${1:-}"
DEV_SIGNING_KEY="${DEV_SIGNING_KEY:-eventplatform-dev-hs256-signing-key-not-a-secret}"
TENANT_ID="${TENANT_ID:-11111111-1111-1111-1111-111111111111}"

DEV_SIGNING_KEY="$DEV_SIGNING_KEY" TENANT_ID="$TENANT_ID" SUB="$SUB" python3 - <<'PY'
import base64, hmac, hashlib, json, os, time, uuid

secret = os.environ["DEV_SIGNING_KEY"]
tenant = os.environ["TENANT_ID"]
sub = os.environ["SUB"] or str(uuid.uuid4())

b64 = lambda b: base64.urlsafe_b64encode(b).rstrip(b"=")
now = int(time.time())
header = {"alg": "HS256", "typ": "JWT"}
payload = {
    "iss": "eventplatform-dev",
    "aud": "eventplatform",
    "iat": now,
    "exp": now + 3600,
    "tenant_id": tenant,
    "sub": sub,
}
signing_input = b64(json.dumps(header).encode()) + b"." + b64(json.dumps(payload).encode())
signature = b64(hmac.new(secret.encode(), signing_input, hashlib.sha256).digest())
print((signing_input + b"." + signature).decode())
PY
