#!/usr/bin/env bash
# get-token-from-obj_id.sh — acquires a delegated Entra access token for the
# A2A API and writes it to .env (read automatically by the REST Client via
# {{$dotenv TOKEN}}).
#
# Prereqs: `az login` already done for the right tenant. Azure CLI must be
# pre-authorized on the API app registration for scope access_as_user.
#
# Usage:  ./get-token-from-obj_id.sh <APP_OBJ_ID> [TENANT_ID]
#   APP_OBJ_ID  (required) object id of the API app registration.
#   TENANT_ID   (optional) defaults to 3ad0b905-34ab-4116-93d9-c1dcc2d35af6.
# Then just run the requests in EchoAgentApi.http — no copy/paste needed.

set -euo pipefail

APP_OBJ_ID="${1:-}"
TENANT_ID="${2:-3ad0b905-34ab-4116-93d9-c1dcc2d35af6}"

if [[ -z "$APP_OBJ_ID" ]]; then
  echo "ERROR: APP_OBJ_ID is required." >&2
  echo "Usage: ./get-token-from-obj_id.sh <APP_OBJ_ID> [TENANT_ID]" >&2
  exit 2
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/.env"

# Ensure we're logged in to the expected tenant (no-op if already signed in).
if ! az account show --query tenantId -o tsv 2>/dev/null | grep -q "$TENANT_ID"; then
  echo "Signing in to tenant $TENANT_ID ..." >&2
  az login --use-device-code --tenant "$TENANT_ID" >/dev/null
fi

# The 5 instructions, in one shot.
APP_ID_URI=$(az ad app show --id "$APP_OBJ_ID" --query "identifierUris[0]" -o tsv | tr -d '\r')
SCOPE_VALUE=$(az ad app show --id "$APP_OBJ_ID" --query "api.oauth2PermissionScopes[0].value" -o tsv | tr -d '\r')
TOKEN=$(az account get-access-token --scope "${APP_ID_URI}/${SCOPE_VALUE}" --query accessToken -o tsv | tr -d '\r\n')

if [[ -z "$TOKEN" ]]; then
  echo "ERROR: failed to acquire token." >&2
  exit 1
fi

# Write the token where the REST Client can read it ({{$dotenv TOKEN}}).
printf 'TOKEN=%s\n' "$TOKEN" > "$ENV_FILE"
chmod 600 "$ENV_FILE"

# Best-effort copy to clipboard (works if a clipboard tool is available).
if command -v clip.exe >/dev/null 2>&1; then
  printf '%s' "$TOKEN" | clip.exe
elif command -v wl-copy >/dev/null 2>&1; then
  printf '%s' "$TOKEN" | wl-copy
elif command -v xclip >/dev/null 2>&1; then
  printf '%s' "$TOKEN" | xclip -selection clipboard
fi

echo "Token written to $ENV_FILE (scope: ${APP_ID_URI}/${SCOPE_VALUE})." >&2
echo "Now run the requests in EchoAgentApi.http — they read it via {{\$dotenv TOKEN}}." >&2

