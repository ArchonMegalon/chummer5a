#!/usr/bin/env bash
set -euo pipefail

API_URL="${CHUMMER_API_BASE_URL:-${CHUMMER_WEB_BASE_URL:-http://127.0.0.1:${CHUMMER_API_PORT:-${CHUMMER_WEB_PORT:-8088}}}}"
UI_URL="${CHUMMER_BLAZOR_BASE_URL:-http://127.0.0.1:${CHUMMER_BLAZOR_PORT:-8089}}"
API_KEY="${CHUMMER_API_KEY:-}"

curl_with_key() {
  local url="$1"
  if [[ -n "$API_KEY" ]]; then
    curl -fsS -H "X-Api-Key: $API_KEY" "$url"
  else
    curl -fsS "$url"
  fi
}

wait_for_url() {
  local url="$1"
  local max_attempts="${2:-25}"
  local sleep_seconds="${3:-1}"

  for ((attempt = 1; attempt <= max_attempts; attempt++)); do
    if curl -fsS "$url" >/dev/null 2>&1; then
      return 0
    fi
    sleep "$sleep_seconds"
  done

  echo "Timed out waiting for $url" >&2
  return 1
}

wait_for_url "$API_URL/api/health"
wait_for_url "$UI_URL/health"

api_health=$(curl_with_key "$API_URL/api/health")
ui_health=$(curl_with_key "$UI_URL/health")
ui_html=$(curl_with_key "$UI_URL/")

if ! grep -q '"ok":true' <<<"$api_health"; then
  echo "API health response did not contain ok=true: $api_health" >&2
  exit 1
fi

if ! grep -q '"head":"blazor"' <<<"$ui_health"; then
  echo "Blazor health response did not contain head=blazor: $ui_health" >&2
  exit 1
fi

if ! grep -q "Chummer Blazor Head" <<<"$ui_html"; then
  echo "Blazor shell marker not found in root page response." >&2
  exit 1
fi

if ! grep -q "_framework/blazor.web.js" <<<"$ui_html"; then
  echo "Blazor framework script marker missing from root page response." >&2
  exit 1
fi

echo "ui E2E completed"
