#!/usr/bin/env bash
set -euo pipefail

MAX_ITERS="${1:-6}"
PORT="${CHUMMER_WEB_PORT:-8088}"
FAILED=0

for ((iter = 1; iter <= MAX_ITERS; iter++)); do
  echo "===== migration slice iteration ${iter}/${MAX_ITERS} ====="

  if docker compose up -d --build chummer-web \
    && CHUMMER_WEB_PORT="$PORT" bash scripts/audit-compliance.sh \
    && CHUMMER_WEB_PORT="$PORT" bash scripts/e2e-live.sh \
    && docker compose --profile test run --build --rm chummer-tests; then
    echo "iteration $iter passed all gates"
    continue
  fi

  echo "iteration $iter failed; continuing to next loop"
  FAILED=$((FAILED + 1))
done

if [[ "$FAILED" -gt 0 ]]; then
  echo "loop finished ${MAX_ITERS} iterations with ${FAILED} failed iteration(s)" >&2
  exit 1
fi

echo "compliance achieved across ${MAX_ITERS}/${MAX_ITERS} iterations"
exit 0
