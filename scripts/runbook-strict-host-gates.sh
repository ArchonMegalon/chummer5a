#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

STRICT_FILTER="${TEST_FILTER:-${1:-}}"
STRICT_FRAMEWORK="${TEST_FRAMEWORK:-${2:-}}"

run_local_tests() {
  echo "== strict local-tests gate =="
  if [[ -n "$STRICT_FILTER" ]]; then
    echo "filter: $STRICT_FILTER"
  fi
  if [[ -n "$STRICT_FRAMEWORK" ]]; then
    echo "framework: $STRICT_FRAMEWORK"
  fi

  RUNBOOK_MODE=local-tests \
  TEST_NUGET_SOFT_FAIL=0 \
  TEST_DISABLE_BUILD_SERVERS=1 \
  TEST_MAX_CPU=1 \
  TEST_FILTER="$STRICT_FILTER" \
  TEST_FRAMEWORK="$STRICT_FRAMEWORK" \
  bash "$REPO_ROOT/scripts/runbook.sh"
}

run_docker_tests() {
  echo "== strict docker-tests gate =="
  if [[ -n "$STRICT_FILTER" ]]; then
    echo "filter: $STRICT_FILTER"
  fi
  if [[ -n "$STRICT_FRAMEWORK" ]]; then
    echo "framework: $STRICT_FRAMEWORK"
  fi

  RUNBOOK_MODE=docker-tests \
  DOCKER_TESTS_SOFT_FAIL=0 \
  DOCKER_TESTS_BUILD="${DOCKER_TESTS_BUILD:-1}" \
  TEST_FILTER="$STRICT_FILTER" \
  TEST_FRAMEWORK="$STRICT_FRAMEWORK" \
  bash "$REPO_ROOT/scripts/runbook.sh"
}

run_local_tests
run_docker_tests

echo "Strict host gates completed successfully."
