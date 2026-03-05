#!/usr/bin/env bash
set -euo pipefail

TARGET="${1:-${CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL:-}}"

if [[ -z "${TARGET}" ]]; then
  echo "Provide a portal base URL or manifest path as the first argument (or set CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL)." >&2
  exit 1
fi

python3 - "$TARGET" <<'PY'
import json
import os
import sys
import urllib.request
from pathlib import Path

raw_target = (sys.argv[1] or "").strip()
if not raw_target:
    raise SystemExit("Manifest verification target was empty.")

require_published_version = os.environ.get("CHUMMER_PORTAL_DOWNLOADS_REQUIRE_PUBLISHED_VERSION", "").strip().lower() == "true"

if raw_target.startswith(("http://", "https://")):
    manifest_url = raw_target.rstrip("/")
    if not manifest_url.endswith("/downloads/releases.json"):
        manifest_url = f"{manifest_url}/downloads/releases.json"

    with urllib.request.urlopen(manifest_url, timeout=30) as response:
        manifest = json.load(response)

    source = manifest_url
else:
    manifest_path = Path(raw_target).expanduser()
    if manifest_path.is_dir():
        manifest_path = manifest_path / "releases.json"

    if not manifest_path.exists():
        raise SystemExit(f"Manifest file not found: {manifest_path}")

    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    source = str(manifest_path)

downloads = manifest.get("downloads") or []
if not isinstance(downloads, list) or not downloads:
    raise SystemExit(f"Manifest at {source} has no downloads.")

version = str(manifest.get("version") or "").strip()
if require_published_version and (not version or version.lower() == "unpublished"):
    raise SystemExit(
        f"Manifest at {source} has unpublished version '{version or '<missing>'}' while CHUMMER_PORTAL_DOWNLOADS_REQUIRE_PUBLISHED_VERSION=true."
    )

print(f"Verified manifest at {source} with {len(downloads)} artifact(s).")
PY
