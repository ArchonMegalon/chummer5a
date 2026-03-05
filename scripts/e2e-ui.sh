#!/usr/bin/env bash
set -euo pipefail

API_URL="${CHUMMER_API_BASE_URL:-${CHUMMER_WEB_BASE_URL:-http://127.0.0.1:${CHUMMER_API_PORT:-${CHUMMER_WEB_PORT:-8088}}}}"
UI_URL="${CHUMMER_BLAZOR_BASE_URL:-http://127.0.0.1:${CHUMMER_BLAZOR_PORT:-8089}}"
PLAYWRIGHT_UI_URL="${CHUMMER_UI_PLAYWRIGHT_BASE_URL:-http://127.0.0.1:${CHUMMER_BLAZOR_PORT:-8089}}"
API_KEY="${CHUMMER_API_KEY:-}"
CURL_ARGS=(--connect-timeout 3 --max-time 20)

curl_with_retries() {
  local max_attempts="${1:-3}"
  shift

  local attempt
  for ((attempt = 1; attempt <= max_attempts; attempt++)); do
    if curl "$@"; then
      return 0
    fi
    sleep 1
  done

  return 1
}

curl_with_key() {
  local url="$1"
  if [[ -n "$API_KEY" ]]; then
    curl_with_retries 3 -fsS "${CURL_ARGS[@]}" -H "X-Api-Key: $API_KEY" "$url"
  else
    curl_with_retries 3 -fsS "${CURL_ARGS[@]}" "$url"
  fi
}

wait_for_url() {
  local url="$1"
  local max_attempts="${2:-25}"
  local sleep_seconds="${3:-1}"

  for ((attempt = 1; attempt <= max_attempts; attempt++)); do
    if curl_with_retries 1 -fsS "${CURL_ARGS[@]}" "$url" >/dev/null 2>&1; then
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

if [[ -n "${CHUMMER_UI_PLAYWRIGHT:-}" ]]; then
  RUN_PLAYWRIGHT="$CHUMMER_UI_PLAYWRIGHT"
elif [[ "${CI:-}" == "true" || "${GITHUB_ACTIONS:-}" == "true" ]]; then
  RUN_PLAYWRIGHT="1"
else
  RUN_PLAYWRIGHT="0"
fi
PLAYWRIGHT_TIMEOUT_SECONDS="${CHUMMER_UI_PLAYWRIGHT_TIMEOUT_SECONDS:-240}"
if [[ "$RUN_PLAYWRIGHT" == "1" ]]; then
  echo "running playwright ui e2e against ${PLAYWRIGHT_UI_URL} (timeout: ${PLAYWRIGHT_TIMEOUT_SECONDS}s)"
  if ! CHUMMER_API_KEY="$API_KEY" CHUMMER_UI_PLAYWRIGHT_BASE_URL="$PLAYWRIGHT_UI_URL" timeout "${PLAYWRIGHT_TIMEOUT_SECONDS}"s docker compose --profile test run --build --rm -T chummer-playwright; then
    echo "playwright ui e2e failed or timed out after ${PLAYWRIGHT_TIMEOUT_SECONDS}s" >&2
    exit 1
  fi
else
  echo "skipping playwright ui e2e (set CHUMMER_UI_PLAYWRIGHT=1 to enable)"
fi

echo "ui E2E completed"
