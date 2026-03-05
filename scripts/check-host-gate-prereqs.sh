#!/usr/bin/env bash
set -euo pipefail

NUGET_ENDPOINT="${NUGET_ENDPOINT:-api.nuget.org:443}"
CHECK_DOCKER="${CHECK_DOCKER:-1}"
CHECK_NUGET="${CHECK_NUGET:-1}"

status=0

is_true() {
  local value
  value="$(echo "${1:-}" | tr '[:upper:]' '[:lower:]')"
  [[ "$value" == "1" || "$value" == "true" || "$value" == "yes" || "$value" == "on" ]]
}

echo "== strict host gate prerequisites =="

if is_true "$CHECK_DOCKER"; then
  if ! command -v docker >/dev/null 2>&1; then
    echo "[FAIL] docker CLI not found."
    status=1
  else
    if docker ps >/tmp/chummer-strict-prereq-docker.log 2>&1; then
      echo "[PASS] docker daemon reachable."
    else
      echo "[FAIL] docker daemon not reachable."
      cat /tmp/chummer-strict-prereq-docker.log || true
      status=1
    fi
  fi
else
  echo "[SKIP] docker prerequisite check disabled."
fi

if is_true "$CHECK_NUGET"; then
  host="${NUGET_ENDPOINT%:*}"
  port="${NUGET_ENDPOINT##*:}"
  if [[ -z "$host" || -z "$port" || "$host" == "$port" ]]; then
    echo "[FAIL] invalid NUGET_ENDPOINT value '$NUGET_ENDPOINT' (expected host:port)."
    status=1
  else
    set +e
    python3 - "$host" "$port" <<'PY' >/tmp/chummer-strict-prereq-nuget.log 2>&1
import socket
import sys

host = sys.argv[1]
port = int(sys.argv[2])
with socket.create_connection((host, port), timeout=3):
    pass
PY
    probe_status=$?
    set -e
    if [[ "$probe_status" -eq 0 ]]; then
      echo "[PASS] nuget endpoint reachable: $NUGET_ENDPOINT"
    else
      echo "[FAIL] nuget endpoint not reachable: $NUGET_ENDPOINT"
      cat /tmp/chummer-strict-prereq-nuget.log || true
      status=1
    fi
  fi
else
  echo "[SKIP] nuget prerequisite check disabled."
fi

if [[ "$status" -eq 0 ]]; then
  echo "Strict host gates are ready."
else
  echo "Strict host gates are NOT ready."
fi

exit "$status"
