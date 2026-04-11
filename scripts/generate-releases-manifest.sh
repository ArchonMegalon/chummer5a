#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

DOWNLOADS_DIR="${DOWNLOADS_DIR:-$REPO_ROOT/Docker/Downloads/files}"
MANIFEST_PATH="${MANIFEST_PATH:-$REPO_ROOT/Docker/Downloads/releases.json}"
PORTAL_MANIFEST_PATH="${PORTAL_MANIFEST_PATH:-$REPO_ROOT/Chummer.Portal/downloads/releases.json}"
PORTAL_DOWNLOADS_DIR="${PORTAL_DOWNLOADS_DIR:-$REPO_ROOT/Chummer.Portal/downloads}"
STARTUP_SMOKE_DIR="${STARTUP_SMOKE_DIR:-}"
RELEASE_VERSION="${RELEASE_VERSION:-unpublished}"
RELEASE_CHANNEL="${RELEASE_CHANNEL:-docker}"
RELEASE_PUBLISHED_AT="${RELEASE_PUBLISHED_AT:-$(date -u +%Y-%m-%dT%H:%M:%SZ)}"
CHUMMER_MACOS_PUBLIC_SHELF_ENABLED="${CHUMMER_MACOS_PUBLIC_SHELF_ENABLED:-false}"

mkdir -p "$(dirname "$MANIFEST_PATH")"
mkdir -p "$(dirname "$PORTAL_MANIFEST_PATH")"
mkdir -p "$DOWNLOADS_DIR"

python3 - "$DOWNLOADS_DIR" "$MANIFEST_PATH" "$RELEASE_VERSION" "$RELEASE_CHANNEL" "$RELEASE_PUBLISHED_AT" "$CHUMMER_MACOS_PUBLIC_SHELF_ENABLED" "$STARTUP_SMOKE_DIR" <<'PY'
import hashlib
import json
import re
import sys
from pathlib import Path

downloads_dir = Path(sys.argv[1])
manifest_path = Path(sys.argv[2])
version = sys.argv[3]
channel = sys.argv[4]
published_at = sys.argv[5]
macos_public_shelf_enabled = sys.argv[6].strip().lower() in {"1", "true", "yes", "on"}
startup_smoke_dir = Path(sys.argv[7]).expanduser() if sys.argv[7].strip() else None

app_labels = {
    "avalonia": "Avalonia Desktop",
    "blazor-desktop": "Blazor Desktop",
}
platform_names = {
    "win-x64": "Windows x64",
    "win-arm64": "Windows ARM64",
    "linux-x64": "Linux x64",
    "linux-arm64": "Linux ARM64",
    "osx-arm64": "macOS ARM64",
    "osx-x64": "macOS x64",
}

pattern = re.compile(
    r"^chummer-(?P<app>avalonia|blazor-desktop)-(?P<rid>.+?)(?:-(?P<flavor>installer|portable))?\.(?P<ext>zip|tar\.gz|exe|deb|dmg)$"
)
downloads = []


def load_startup_smoke_receipts(directory: Path | None) -> list[dict[str, str]]:
    if directory is None or not directory.exists():
        return []

    receipts = []
    for receipt_path in sorted(directory.rglob("startup-smoke-*.receipt.json")):
        try:
            loaded = json.loads(receipt_path.read_text(encoding="utf-8-sig"))
        except (OSError, json.JSONDecodeError):
            continue
        if not isinstance(loaded, dict):
            continue
        head = str(loaded.get("headId") or "").strip()
        platform = str(loaded.get("platform") or "").strip().lower()
        arch = str(loaded.get("arch") or "").strip().lower()
        artifact_digest = str(loaded.get("artifactDigest") or "").strip().lower()
        if not head or not platform or not arch:
            continue
        receipts.append(
            {
                "head": head,
                "platform": platform,
                "arch": arch,
                "artifactDigest": artifact_digest,
            }
        )
    return receipts


def normalize_flavor(raw_flavor: str | None, ext: str) -> str:
    if raw_flavor:
        return raw_flavor
    if ext in {"zip", "tar.gz"}:
        return "archive"
    if ext in {"deb", "dmg"}:
        return "installer"
    return "portable"


def resolve_head(app: str) -> str:
    return "flagship" if app == "avalonia" else "fallback"


def build_platform_label(app: str, rid: str, flavor: str) -> str:
    flavor_label = {
        "installer": "Installer",
        "portable": "Portable",
        "archive": "Archive",
    }.get(flavor, flavor.title())
    return f"{app_labels.get(app, app)} {platform_names.get(rid, rid)} {flavor_label}"


def is_public_shelf_artifact(rid: str) -> bool:
    if rid.lower().startswith("osx"):
        return macos_public_shelf_enabled
    return True


startup_smoke_receipts = load_startup_smoke_receipts(startup_smoke_dir)


def has_startup_smoke_proof(app: str, rid: str, flavor: str, sha256: str) -> bool:
    if flavor != "installer" or not startup_smoke_receipts:
        return True
    platform = {
        "win-x64": "windows",
        "win-arm64": "windows",
        "linux-x64": "linux",
        "linux-arm64": "linux",
        "osx-x64": "macos",
        "osx-arm64": "macos",
    }.get(rid, "")
    arch = {
        "win-x64": "x64",
        "linux-x64": "x64",
        "osx-x64": "x64",
        "win-arm64": "arm64",
        "linux-arm64": "arm64",
        "osx-arm64": "arm64",
    }.get(rid, "")
    matching_receipts = [
        receipt
        for receipt in startup_smoke_receipts
        if receipt["head"] == app and receipt["platform"] == platform and receipt["arch"] == arch
    ]
    if not matching_receipts:
        return False
    expected_digest = f"sha256:{sha256.lower()}"
    return any(not receipt["artifactDigest"] or receipt["artifactDigest"] == expected_digest for receipt in matching_receipts)

for artifact in sorted(downloads_dir.iterdir()):
    if not artifact.is_file():
        continue

    match = pattern.match(artifact.name)
    if not match:
        continue

    app = match.group("app")
    rid = match.group("rid")
    ext = match.group("ext")
    flavor = normalize_flavor(match.group("flavor"), ext)
    if not is_public_shelf_artifact(rid):
        continue
    sha256 = hashlib.sha256(artifact.read_bytes()).hexdigest()
    if not has_startup_smoke_proof(app, rid, flavor, sha256):
        continue
    size_bytes = artifact.stat().st_size
    downloads.append(
        {
            "id": f"{app}-{rid}-{flavor}",
            "platform": build_platform_label(app, rid, flavor),
            "url": f"/downloads/files/{artifact.name}",
            "sha256": sha256,
            "sizeBytes": size_bytes,
            "format": ext,
            "flavor": flavor,
            "app": app,
            "rid": rid,
            "head": resolve_head(app),
            "recommended": app == "avalonia" and flavor == "installer",
        }
    )

if not downloads:
    version = "unpublished"

manifest = {
    "version": version,
    "channel": channel,
    "publishedAt": published_at,
    "downloads": downloads,
}

manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
print(f"wrote {manifest_path} with {len(downloads)} download entry(ies)")
PY

resolved_manifest_path="$(realpath "$MANIFEST_PATH")"
resolved_portal_manifest_path="$(realpath -m "$PORTAL_MANIFEST_PATH")"
if [[ "$resolved_manifest_path" == "$resolved_portal_manifest_path" ]]; then
  echo "portal manifest path matches manifest output; skipped secondary sync"
else
  cp "$MANIFEST_PATH" "$PORTAL_MANIFEST_PATH"
  echo "synced portal manifest -> $PORTAL_MANIFEST_PATH"

  portal_files_dir="$PORTAL_DOWNLOADS_DIR/files"
  mkdir -p "$portal_files_dir"
  is_public_artifact() {
    local artifact_name
    artifact_name="$(basename "$1")"
    if [[ "$artifact_name" == chummer-*-osx-* ]] && [[ "${CHUMMER_MACOS_PUBLIC_SHELF_ENABLED,,}" != "1" && "${CHUMMER_MACOS_PUBLIC_SHELF_ENABLED,,}" != "true" && "${CHUMMER_MACOS_PUBLIC_SHELF_ENABLED,,}" != "yes" && "${CHUMMER_MACOS_PUBLIC_SHELF_ENABLED,,}" != "on" ]]; then
      return 1
    fi
    return 0
  }
  mapfile -t portal_artifacts < <(
    while IFS= read -r artifact; do
      [[ -f "$artifact" ]] || continue
      if is_public_artifact "$artifact"; then
        printf '%s\n' "$artifact"
      fi
    done < <(find "$DOWNLOADS_DIR" -maxdepth 1 -type f \
      \( -name "chummer-*.zip" -o -name "chummer-*.tar.gz" -o -name "chummer-*.exe" -o -name "chummer-*.deb" -o -name "chummer-*.dmg" \) \
      | sort)
  )
  if [[ "${#portal_artifacts[@]}" -gt 0 ]]; then
    rm -f "$portal_files_dir"/chummer-*.zip "$portal_files_dir"/chummer-*.tar.gz "$portal_files_dir"/chummer-*.exe "$portal_files_dir"/chummer-*.deb "$portal_files_dir"/chummer-*.dmg
    cp "${portal_artifacts[@]}" "$portal_files_dir"/
    echo "synced ${#portal_artifacts[@]} local portal artifact(s) -> $portal_files_dir"
  else
    echo "no local desktop artifacts found in $DOWNLOADS_DIR for portal file sync"
  fi
fi
