# Self-Hosted Downloads Runbook

Purpose: publish desktop artifacts to a self-hosted downloads surface and verify that `/downloads/releases.json` serves non-empty artifacts.

## Prerequisites

1. Desktop bundle exists (`desktop-download-bundle` layout):
`releases.json` and `files/chummer-*.zip|tar.gz`.
2. Portal serves `/downloads/releases.json` from your storage topology.
3. Use preapproved runbook/script paths from repository root (`/docker/chummer5a`).

## Mode A: Filesystem Deploy (shared mount)

Repository variables:
1. `CHUMMER_PORTAL_DOWNLOADS_DEPLOY_DIR`
2. `CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL`

Workflow path:
1. Run workflow `Desktop Downloads Matrix` with `deploy_portal_downloads=true`.
2. Deploy job `deploy-downloads` syncs bundle using `scripts/publish-download-bundle.sh`.
3. Job verifies local deployed manifest and live manifest URL.

Manual path:
1. `RUNBOOK_MODE=downloads-sync DOWNLOAD_BUNDLE_DIR=<bundleDir> DOWNLOAD_DEPLOY_DIR=<deployDir> DOWNLOADS_SYNC_DEPLOY_MODE=1 DOWNLOADS_SYNC_VERIFY_TARGET=<portalBaseOrManifestUrl> bash scripts/runbook.sh`
2. `RUNBOOK_MODE=downloads-verify DOWNLOADS_VERIFY_LINKS=1 DOWNLOADS_VERIFY_TARGET=<portalBaseOrManifestUrl> bash scripts/runbook.sh`

## Mode B: Object Storage Deploy (S3/R2 compatible)

Repository variables:
1. `CHUMMER_PORTAL_DOWNLOADS_S3_URI`
2. `CHUMMER_PORTAL_DOWNLOADS_S3_LATEST_URI` (optional)
3. `CHUMMER_PORTAL_DOWNLOADS_S3_ENDPOINT_URL` (optional; required for many R2/S3-compatible endpoints)
4. `CHUMMER_PORTAL_DOWNLOADS_S3_REGION` (optional, defaults to `us-east-1`)
5. `CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL`

Repository secrets:
1. `CHUMMER_PORTAL_DOWNLOADS_AWS_ACCESS_KEY_ID`
2. `CHUMMER_PORTAL_DOWNLOADS_AWS_SECRET_ACCESS_KEY`
3. `CHUMMER_PORTAL_DOWNLOADS_AWS_SESSION_TOKEN` (optional)

Workflow path:
1. Run workflow `Desktop Downloads Matrix` with `deploy_portal_downloads=true`.
2. Deploy job `deploy-downloads-object-storage` syncs bundle using `scripts/publish-download-bundle-s3.sh`.
3. Job verifies live manifest URL.

Manual path:
1. `RUNBOOK_MODE=downloads-sync-s3 DOWNLOAD_BUNDLE_DIR=<bundleDir> CHUMMER_PORTAL_DOWNLOADS_S3_URI=<s3://bucket/path> CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL=<portalBaseOrManifestUrl> [CHUMMER_PORTAL_DOWNLOADS_S3_ENDPOINT_URL=<endpoint>] bash scripts/runbook.sh`
2. `RUNBOOK_MODE=downloads-verify DOWNLOADS_VERIFY_LINKS=1 DOWNLOADS_VERIFY_TARGET=<portalBaseOrManifestUrl> bash scripts/runbook.sh`

## Strict Test Gate Commands (host-side)

Use these when you want hard failures instead of soft-skips.

Prerequisite probe:
1. `RUNBOOK_MODE=host-prereqs bash scripts/runbook.sh`

Single wrapper command:
1. `bash scripts/runbook-strict-host-gates.sh [optionalTestFilter] [optionalFramework]`

Local tests:
1. `RUNBOOK_MODE=local-tests TEST_NUGET_SOFT_FAIL=0 TEST_DISABLE_BUILD_SERVERS=1 TEST_MAX_CPU=1 bash scripts/runbook.sh`
2. Optional offline attempt after successful restore cache: `RUNBOOK_MODE=local-tests TEST_NO_RESTORE=1 TEST_DISABLE_BUILD_SERVERS=1 TEST_MAX_CPU=1 bash scripts/runbook.sh`

Docker tests:
1. `RUNBOOK_MODE=docker-tests DOCKER_TESTS_SOFT_FAIL=0 DOCKER_TESTS_BUILD=1 bash scripts/runbook.sh`

## Expected Verification Outcome

1. `/downloads/releases.json` has `downloads` with at least one artifact.
2. `version` is not `"unpublished"` in deployment mode.
3. When `CHUMMER_PORTAL_DOWNLOADS_VERIFY_LINKS=true` (or `DOWNLOADS_VERIFY_LINKS=1`), each artifact URL/file in manifest verification is reachable.
4. Portal `/downloads/` renders artifact links that return HTTP 200.
