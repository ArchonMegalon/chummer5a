#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

RUNBOOK_MODE="${RUNBOOK_MODE:-${1:-tunnel}}"
RUNBOOK_ARG_FRAMEWORK="${2:-}"
RUNBOOK_ARG_FILTER="${3:-}"
TUNNEL_CONTAINER="${TUNNEL_CONTAINER:-cloudflared_v2}"
DOCKER_NETWORK="${DOCKER_NETWORK:-arr_net_v2}"
UPSTREAM_PRIMARY="${UPSTREAM_PRIMARY:-http://172.17.0.1:8088}"
UPSTREAM_UI="${UPSTREAM_UI:-http://172.17.0.1:8089}"
UPSTREAM_LEGACY="${UPSTREAM_LEGACY:-http://chummer-web:8080}"
UPSTREAM_UI_SERVICE="${UPSTREAM_UI_SERVICE:-http://chummer-blazor:8080}"
UPSTREAM_HOST_INTERNAL="${UPSTREAM_HOST_INTERNAL:-http://host.docker.internal:8088}"

if ! command -v rg >/dev/null 2>&1; then
  echo "ripgrep (rg) is required for this runbook." >&2
  exit 1
fi

if [[ "$RUNBOOK_MODE" == "migration" ]]; then
  LOOPS="${MIGRATION_LOOPS:-1}"
  LOG_FILE="${RUNBOOK_LOG_FILE:-/tmp/migration-loop-runbook.log}"
  set +e
  bash scripts/migration-loop.sh "$LOOPS" 2>&1 | tee "$LOG_FILE"
  status=${PIPESTATUS[0]}
  set -e
  echo
  echo "== migration failure extract =="
  rg -n "Failed|failed|\\[xUnit.net\\]|error|Test summary|Stack Trace" "$LOG_FILE" | tail -n 200 || true
  exit "$status"
fi

if [[ "$RUNBOOK_MODE" == "local-tests" ]]; then
  TEST_PROJECT="${TEST_PROJECT:-Chummer.Tests/Chummer.Tests.csproj}"
  TEST_CONFIGURATION="${TEST_CONFIGURATION:-Release}"
  TEST_FRAMEWORK="${TEST_FRAMEWORK:-$RUNBOOK_ARG_FRAMEWORK}"
  TEST_FILTER="${TEST_FILTER:-$RUNBOOK_ARG_FILTER}"
  TEST_MAX_CPU="${TEST_MAX_CPU:-1}"
  TEST_DISABLE_BUILD_SERVERS="${TEST_DISABLE_BUILD_SERVERS:-1}"
  TEST_NO_RESTORE="${TEST_NO_RESTORE:-0}"
  TEST_NO_BUILD="${TEST_NO_BUILD:-0}"
  TEST_LOG_FILE="${TEST_LOG_FILE:-/tmp/chummer-local-tests.log}"
  export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/tmp}"
  export DOTNET_SKIP_FIRST_TIME_EXPERIENCE="${DOTNET_SKIP_FIRST_TIME_EXPERIENCE:-1}"
  export DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER="${DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER:-1}"
  export MSBUILDDISABLENODEREUSE="${MSBUILDDISABLENODEREUSE:-1}"
  framework_args=()
  filter_args=()
  cpu_args=()
  server_args=()
  restore_args=()
  build_args=()
  if [[ -n "$TEST_FRAMEWORK" ]]; then
    framework_args=(-f "$TEST_FRAMEWORK")
  fi
  if [[ -n "$TEST_FILTER" ]]; then
    filter_args=(--filter "$TEST_FILTER")
  fi
  if [[ -n "$TEST_MAX_CPU" ]]; then
    cpu_args=(-m:"$TEST_MAX_CPU")
  fi
  if [[ "$TEST_DISABLE_BUILD_SERVERS" == "1" || "$TEST_DISABLE_BUILD_SERVERS" == "true" || "$TEST_DISABLE_BUILD_SERVERS" == "TRUE" ]]; then
    server_args=(--disable-build-servers)
  fi
  if [[ "$TEST_NO_RESTORE" == "1" || "$TEST_NO_RESTORE" == "true" || "$TEST_NO_RESTORE" == "TRUE" ]]; then
    restore_args=(--no-restore)
  fi
  if [[ "$TEST_NO_BUILD" == "1" || "$TEST_NO_BUILD" == "true" || "$TEST_NO_BUILD" == "TRUE" ]]; then
    build_args=(--no-build)
  fi
  set +e
  dotnet test "$TEST_PROJECT" -c "$TEST_CONFIGURATION" "${framework_args[@]}" "${filter_args[@]}" "${cpu_args[@]}" "${server_args[@]}" "${restore_args[@]}" "${build_args[@]}" --logger "console;verbosity=normal" 2>&1 | tee "$TEST_LOG_FILE"
  status=${PIPESTATUS[0]}
  set -e
  echo
  echo "== local test failure extract =="
  rg -n "^\\s*Failed\\s|\\[xUnit.net\\]|Total tests:|Passed!|Failed!|Stack Trace|Error Message" "$TEST_LOG_FILE" | tail -n 200 || true
  exit "$status"
fi

if [[ "$RUNBOOK_MODE" == "desktop-gate" ]]; then
  status=0

  require_path() {
    local path="$1"
    if [[ ! -e "$path" ]]; then
      echo "missing path: $path" >&2
      status=1
    fi
  }

  require_match() {
    local pattern="$1"
    local path="$2"
    if ! rg -q -- "$pattern" "$path"; then
      echo "missing pattern '$pattern' in $path" >&2
      status=1
    fi
  }

  require_no_match() {
    local pattern="$1"
    local path="$2"
    if rg -q -- "$pattern" "$path"; then
      echo "forbidden pattern '$pattern' found in $path" >&2
      status=1
    fi
  }

  require_path "Chummer.Blazor.Desktop/Chummer.Blazor.Desktop.csproj"
  require_path "Chummer.Blazor.Desktop/Program.cs"
  require_path "Chummer.Desktop.Runtime/ServiceCollectionDesktopRuntimeExtensions.cs"
  require_path "Chummer.Blazor.Desktop/wwwroot/index.html"
  require_path "scripts/validate-amend-manifests.sh"

  require_match "Chummer.Blazor.Desktop\\\\Chummer.Blazor.Desktop.csproj" "Chummer.sln"
  require_match "Photino.Blazor" "Chummer.Blazor.Desktop/Chummer.Blazor.Desktop.csproj"
  require_match "RootComponents.Add<App>\\(\"app\"\\)" "Chummer.Blazor.Desktop/Program.cs"
  require_match "AddChummerLocalRuntimeClient" "Chummer.Blazor.Desktop/Program.cs"
  require_match "CHUMMER_CLIENT_MODE" "Chummer.Desktop.Runtime/ServiceCollectionDesktopRuntimeExtensions.cs"
  require_match "CHUMMER_DESKTOP_CLIENT_MODE" "Chummer.Desktop.Runtime/ServiceCollectionDesktopRuntimeExtensions.cs"
  require_match "CHUMMER_API_BASE_URL" "Chummer.Desktop.Runtime/ServiceCollectionDesktopRuntimeExtensions.cs"
  require_match "Chummer.Blazor.Desktop" "Chummer.Tests/Compliance/ArchitectureGuardrailTests.cs"
  require_match "\\{92C5A638-B7DB-4D42-BC96-C11A063D0EF5\\}\\.Release\\|Any CPU\\.Build\\.0" "Chummer.sln"
  require_match "Chummer.Blazor.Desktop/Chummer.Blazor.Desktop.csproj" ".github/workflows/desktop-downloads-matrix.yml"
  require_match "Chummer.Application/\\*\\*" ".github/workflows/desktop-downloads-matrix.yml"
  require_match "Chummer.Core/\\*\\*" ".github/workflows/desktop-downloads-matrix.yml"
  require_match "Chummer.Desktop.Runtime/\\*\\*" ".github/workflows/desktop-downloads-matrix.yml"
  require_match "Chummer.Infrastructure/\\*\\*" ".github/workflows/desktop-downloads-matrix.yml"
  require_match "Chummer.Portal/\\*\\*" ".github/workflows/desktop-downloads-matrix.yml"
  require_match "scripts/generate-releases-manifest.sh" ".github/workflows/desktop-downloads-matrix.yml"
  require_match "scripts/publish-download-bundle.sh" ".github/workflows/desktop-downloads-matrix.yml"
  require_match "scripts/validate-amend-manifests.sh" ".github/workflows/desktop-downloads-matrix.yml"
  require_match "rid: osx-x64" ".github/workflows/desktop-downloads-matrix.yml"
  require_match "chummer-\\(\\?P<app>avalonia\\|blazor-desktop\\)-" "scripts/generate-releases-manifest.sh"
  require_match "\"osx-x64\": \"macOS x64\"" "scripts/generate-releases-manifest.sh"
  require_match "\"id\": f\"\\{app\\}-\\{rid\\}\"" "scripts/generate-releases-manifest.sh"
  require_match "Task<ShellBootstrapSnapshot> GetShellBootstrapAsync\\(string\\? rulesetId, CancellationToken ct\\);" "Chummer.Presentation/IChummerClient.cs"
  require_no_match "GetShellBootstrapAsync\\(string\\? rulesetId, CancellationToken ct\\)\\s*\\{" "Chummer.Presentation/IChummerClient.cs"
  require_no_match "GetShellBootstrapAsync\\(string\\? rulesetId, CancellationToken ct\\)\\s*=>" "Chummer.Presentation/IChummerClient.cs"
  require_match "RUNBOOK_MODE\" == \"downloads-manifest\"" "scripts/runbook.sh"
  require_match "RUNBOOK_MODE\" == \"downloads-sync\"" "scripts/runbook.sh"
  require_match "RUNBOOK_MODE\" == \"amend-checksums\"" "scripts/runbook.sh"
  require_match "bash scripts/generate-releases-manifest.sh" "scripts/runbook.sh"
  require_match "bash scripts/publish-download-bundle.sh" "scripts/runbook.sh"
  require_match "bash scripts/validate-amend-manifests.sh" "scripts/runbook.sh"
  require_match "Docker/Downloads/releases.json" "scripts/generate-releases-manifest.sh"
  require_match "Chummer.Portal/downloads/releases.json" "scripts/generate-releases-manifest.sh"
  require_match "CHUMMER_PORTAL_DOWNLOADS_DEPLOY_DIR" ".github/workflows/desktop-downloads-matrix.yml"
  require_match "deploy-downloads" ".github/workflows/desktop-downloads-matrix.yml"

  if [[ "$status" -ne 0 ]]; then
    echo "desktop-gate checks failed" >&2
    exit "$status"
  fi

  echo "desktop-gate checks passed"
  exit 0
fi

if [[ "$RUNBOOK_MODE" == "desktop-build" ]]; then
  DESKTOP_PROJECT="${DESKTOP_PROJECT:-${RUNBOOK_ARG_FRAMEWORK:-Chummer.Blazor.Desktop/Chummer.Blazor.Desktop.csproj}}"
  DESKTOP_FRAMEWORK="${DESKTOP_FRAMEWORK:-${RUNBOOK_ARG_FILTER:-net10.0}}"
  DESKTOP_LOG_FILE="${DESKTOP_LOG_FILE:-/tmp/chummer-desktop-build.log}"
  set +e
  docker compose run --build --rm chummer-tests sh -lc \
    "cd /src && dotnet build '$DESKTOP_PROJECT' -c Release -f '$DESKTOP_FRAMEWORK' --nologo" \
    2>&1 | tee "$DESKTOP_LOG_FILE"
  status=${PIPESTATUS[0]}
  set -e
  echo
  echo "== desktop build extract =="
  rg -n "Build succeeded|Build FAILED|error CS|error NU|error :" "$DESKTOP_LOG_FILE" | tail -n 200 || true
  exit "$status"
fi

if [[ "$RUNBOOK_MODE" == "amend-checksums" ]]; then
  AMEND_TARGET="${AMEND_TARGET:-${RUNBOOK_ARG_FRAMEWORK:-Docker/Amends}}"
  AMEND_CHECKSUM_LOG_FILE="${AMEND_CHECKSUM_LOG_FILE:-/tmp/chummer-amend-checksums.log}"
  set +e
  bash scripts/validate-amend-manifests.sh "$AMEND_TARGET" 2>&1 | tee "$AMEND_CHECKSUM_LOG_FILE"
  status=${PIPESTATUS[0]}
  set -e
  echo
  echo "== amend checksum validation summary =="
  rg -n "Validated|ERROR:" "$AMEND_CHECKSUM_LOG_FILE" | tail -n 200 || true
  exit "$status"
fi

if [[ "$RUNBOOK_MODE" == "downloads-manifest" ]]; then
  MANIFEST_LOG_FILE="${MANIFEST_LOG_FILE:-/tmp/chummer-downloads-manifest.log}"
  set +e
  bash scripts/generate-releases-manifest.sh 2>&1 | tee "$MANIFEST_LOG_FILE"
  status=${PIPESTATUS[0]}
  set -e
  echo
  echo "== manifest preview =="
  if [[ -f Docker/Downloads/releases.json ]]; then
    cat Docker/Downloads/releases.json
  else
    echo "Docker/Downloads/releases.json not found"
  fi
  exit "$status"
fi

if [[ "$RUNBOOK_MODE" == "downloads-sync" ]]; then
  DOWNLOAD_BUNDLE_DIR="${DOWNLOAD_BUNDLE_DIR:-${RUNBOOK_ARG_FRAMEWORK:-$REPO_ROOT/dist}}"
  DOWNLOAD_DEPLOY_DIR="${DOWNLOAD_DEPLOY_DIR:-${RUNBOOK_ARG_FILTER:-$REPO_ROOT/Docker/Downloads}}"
  DOWNLOADS_SYNC_DEPLOY_MODE="${DOWNLOADS_SYNC_DEPLOY_MODE:-0}"
  DOWNLOADS_SYNC_VERIFY_TARGET="${DOWNLOADS_SYNC_VERIFY_TARGET:-${CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL:-}}"
  SYNC_LOG_FILE="${SYNC_LOG_FILE:-/tmp/chummer-downloads-sync.log}"
  if [[ "$DOWNLOADS_SYNC_DEPLOY_MODE" == "1" || "$DOWNLOADS_SYNC_DEPLOY_MODE" == "true" || "$DOWNLOADS_SYNC_DEPLOY_MODE" == "TRUE" ]]; then
    if [[ -z "$DOWNLOADS_SYNC_VERIFY_TARGET" ]]; then
      echo "downloads-sync deploy mode requires DOWNLOADS_SYNC_VERIFY_TARGET or CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL." >&2
      exit 1
    fi
    export CHUMMER_PORTAL_DOWNLOADS_DEPLOY_ENABLED=true
    export CHUMMER_PORTAL_DOWNLOADS_REQUIRE_PUBLISHED_VERSION=true
    export CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL="$DOWNLOADS_SYNC_VERIFY_TARGET"
  fi
  set +e
  bash scripts/publish-download-bundle.sh "$DOWNLOAD_BUNDLE_DIR" "$DOWNLOAD_DEPLOY_DIR" 2>&1 | tee "$SYNC_LOG_FILE"
  status=${PIPESTATUS[0]}
  set -e
  echo
  echo "== synced manifest =="
  if [[ -f "$DOWNLOAD_DEPLOY_DIR/releases.json" ]]; then
    cat "$DOWNLOAD_DEPLOY_DIR/releases.json"
  else
    echo "$DOWNLOAD_DEPLOY_DIR/releases.json not found"
  fi
  if [[ "$DOWNLOADS_SYNC_DEPLOY_MODE" == "1" || "$DOWNLOADS_SYNC_DEPLOY_MODE" == "true" || "$DOWNLOADS_SYNC_DEPLOY_MODE" == "TRUE" ]]; then
    echo
    echo "== deployment-mode verification summary =="
    rg -n "Verified manifest at" "$SYNC_LOG_FILE" | tail -n 20 || true
  fi
  exit "$status"
fi

if [[ "$RUNBOOK_MODE" == "downloads-verify" ]]; then
  DOWNLOADS_VERIFY_TARGET="${DOWNLOADS_VERIFY_TARGET:-${RUNBOOK_ARG_FRAMEWORK:-${CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL:-}}}"
  VERIFY_LOG_FILE="${VERIFY_LOG_FILE:-/tmp/chummer-downloads-verify.log}"
  if [[ -z "$DOWNLOADS_VERIFY_TARGET" ]]; then
    echo "Set DOWNLOADS_VERIFY_TARGET, CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL, or pass a URL/path as arg #2." >&2
    exit 1
  fi
  set +e
  bash scripts/verify-releases-manifest.sh "$DOWNLOADS_VERIFY_TARGET" 2>&1 | tee "$VERIFY_LOG_FILE"
  status=${PIPESTATUS[0]}
  set -e
  echo
  echo "== manifest verification summary =="
  rg -n "Verified manifest|has no downloads|not found|empty" "$VERIFY_LOG_FILE" | tail -n 200 || true
  exit "$status"
fi

if [[ "$RUNBOOK_MODE" == "ui-e2e" ]]; then
  UI_E2E_LOG_FILE="${UI_E2E_LOG_FILE:-/tmp/chummer-ui-e2e.log}"
  export CHUMMER_UI_PLAYWRIGHT=1
  set +e
  bash scripts/e2e-ui.sh 2>&1 | tee "$UI_E2E_LOG_FILE"
  status=${PIPESTATUS[0]}
  set -e
  echo
  echo "== ui e2e summary =="
  rg -n "running playwright ui e2e|playwright ui e2e failed|ui E2E completed|Timed out waiting|request failed|skipping ui e2e|skipping playwright ui e2e|permission denied while trying to connect to the Docker daemon socket|operation not permitted" "$UI_E2E_LOG_FILE" | tail -n 200 || true
  exit "$status"
fi

if [[ "$RUNBOOK_MODE" == "portal-e2e" ]]; then
  PORTAL_E2E_LOG_FILE="${PORTAL_E2E_LOG_FILE:-/tmp/chummer-portal-e2e.log}"
  export CHUMMER_PORTAL_PLAYWRIGHT=1
  set +e
  bash scripts/e2e-portal.sh 2>&1 | tee "$PORTAL_E2E_LOG_FILE"
  status=${PIPESTATUS[0]}
  set -e
  echo
  echo "== portal e2e summary =="
  rg -n "running portal playwright e2e|portal playwright e2e failed|portal e2e completed|skipping portal e2e|skipping portal playwright e2e|permission denied while trying to connect to the Docker daemon socket|operation not permitted" "$PORTAL_E2E_LOG_FILE" | tail -n 200 || true
  exit "$status"
fi

if [[ "$RUNBOOK_MODE" == "docker-tests" ]]; then
  TEST_PROJECT="${TEST_PROJECT:-Chummer.Tests/Chummer.Tests.csproj}"
  TEST_FRAMEWORK="${TEST_FRAMEWORK:-${RUNBOOK_ARG_FRAMEWORK:-net10.0}}"
  TEST_FILTER="${TEST_FILTER:-$RUNBOOK_ARG_FILTER}"
  TEST_LOG_FILE="${TEST_LOG_FILE:-/tmp/chummer-docker-tests.log}"
  DOCKER_TESTS_BUILD="${DOCKER_TESTS_BUILD:-1}"
  framework_arg=""
  filter_arg=""
  build_arg=""
  if [[ -n "$TEST_FRAMEWORK" ]]; then
    framework_arg="-f $TEST_FRAMEWORK"
  fi
  if [[ -n "$TEST_FILTER" ]]; then
    filter_arg="--filter \"$TEST_FILTER\""
  fi
  if [[ "$DOCKER_TESTS_BUILD" == "1" || "$DOCKER_TESTS_BUILD" == "true" || "$DOCKER_TESTS_BUILD" == "TRUE" ]]; then
    build_arg="--build"
  fi
  set +e
  docker compose run $build_arg --rm chummer-tests sh -lc \
    "cd /src && dotnet test '$TEST_PROJECT' -c Release $framework_arg $filter_arg --logger \"console;verbosity=normal\"" \
    2>&1 | tee "$TEST_LOG_FILE"
  status=${PIPESTATUS[0]}
  set -e
  echo
  echo "== docker test failure extract =="
  rg -n "^\\s*Failed\\s|\\[xUnit.net\\]|Total tests:|Passed!|Failed!|Stack Trace|Error Message|Test Run Failed" "$TEST_LOG_FILE" | tail -n 200 || true
  exit "$status"
fi

if [[ "$RUNBOOK_MODE" == "push" ]]; then
  RUNBOOK_PUSH_ENABLE="${RUNBOOK_PUSH_ENABLE:-0}"
  if [[ "$RUNBOOK_PUSH_ENABLE" != "1" && "$RUNBOOK_PUSH_ENABLE" != "true" && "$RUNBOOK_PUSH_ENABLE" != "TRUE" ]]; then
    echo "push mode is disabled by default."
    echo "Set RUNBOOK_PUSH_ENABLE=1 to run an explicit push from this runbook."
    echo "Example: RUNBOOK_MODE=push RUNBOOK_PUSH_ENABLE=1 bash scripts/runbook.sh"
    exit 2
  fi

  git_cmd=(git --git-dir="$REPO_ROOT/.git" --work-tree="$REPO_ROOT")
  RUNBOOK_PUSH_REMOTE="${RUNBOOK_PUSH_REMOTE:-origin}"
  RUNBOOK_PUSH_REF="${RUNBOOK_PUSH_REF:-}"
  BRANCH_NAME="$(${git_cmd[@]} rev-parse --abbrev-ref HEAD)"
  REF_SPEC="${RUNBOOK_PUSH_REF:-$BRANCH_NAME}"

  echo "== push mode =="
  echo "branch: $BRANCH_NAME"
  echo "status: $(${git_cmd[@]} status --short --branch | head -n 1)"
  echo "remote: $RUNBOOK_PUSH_REMOTE"
  echo "refspec: $REF_SPEC"

  "${git_cmd[@]}" push "$RUNBOOK_PUSH_REMOTE" "$REF_SPEC"
  echo "push completed"
  exit "$?"
fi

echo "== docker ps (chummer/cloudflared) =="
docker ps --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}' | rg -i 'chummer|cloudflared' || true

echo
echo "== recent cloudflared config/events for chummer (24h) =="
docker logs --since 24h "$TUNNEL_CONTAINER" 2>&1 \
  | rg -n 'Updated to new configuration|chummer\.girschele\.com|originService=.*chummer|lookup chummer-web|Unable to reach the origin service' -i \
  | tail -n 200 || true

echo
echo "== network probe from Docker network: $DOCKER_NETWORK =="
docker run --rm \
  --network "$DOCKER_NETWORK" \
  -e U1="$UPSTREAM_PRIMARY" \
  -e U2="$UPSTREAM_UI" \
  -e U3="$UPSTREAM_LEGACY" \
  -e U4="$UPSTREAM_UI_SERVICE" \
  -e U5="$UPSTREAM_HOST_INTERNAL" \
  busybox sh -lc '
for u in "$U1" "$U2" "$U3" "$U4" "$U5"; do
  echo "--- origin: $u"
  for p in / /api/health /api/info; do
    echo "GET $p"
    wget -qSO- --timeout=3 "$u$p" -O - 2>&1 || true
    echo
  done
  echo
 done
'
