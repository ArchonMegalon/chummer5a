#!/usr/bin/env bash
set -euo pipefail

CHUMMER_API_KEY="${CHUMMER_API_KEY:-}"

if [[ -n "$CHUMMER_API_KEY" ]]; then
  export CHUMMER_API_KEY
fi

docker compose --profile portal up -d --build chummer-api chummer-blazor-portal chummer-portal

docker compose --profile test --profile portal run --rm chummer-playwright \
  node /work/scripts/e2e-portal.cjs

echo "portal e2e completed"
