#!/usr/bin/env bash
set -euo pipefail

CHUMMER_API_KEY="${CHUMMER_API_KEY:-}"
PORTAL_PLAYWRIGHT_TIMEOUT_SECONDS="${CHUMMER_PORTAL_E2E_TIMEOUT_SECONDS:-240}"

if [[ -n "$CHUMMER_API_KEY" ]]; then
  export CHUMMER_API_KEY
fi

docker compose --profile portal up -d --build chummer-api chummer-blazor-portal chummer-avalonia-browser chummer-portal

echo "running portal playwright e2e (timeout: ${PORTAL_PLAYWRIGHT_TIMEOUT_SECONDS}s)"
if ! timeout "${PORTAL_PLAYWRIGHT_TIMEOUT_SECONDS}"s docker compose --profile test --profile portal run --build --rm chummer-playwright-portal; then
  echo "portal playwright e2e failed or timed out after ${PORTAL_PLAYWRIGHT_TIMEOUT_SECONDS}s" >&2
  exit 1
fi

echo "portal e2e completed"
