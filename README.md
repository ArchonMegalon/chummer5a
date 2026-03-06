<p align="center"><h1>Chummer 5</h1></p>
<p align="center"><img src="https://i.ibb.co/y0WC3j9/logo.png"></p>

[![Github Latest Release Date](https://img.shields.io/github/release-date/chummer5a/chummer5a?label=Latest%20Milestone%20Release)](https://github.com/chummer5a/chummer5a/releases/latest)
[![GitHub Issues](https://img.shields.io/github/issues/chummer5a/chummer5a.svg)](https://github.com/chummer5a/chummer5a/issues)
[![Build status](https://ci.appveyor.com/api/projects/status/wf0jbqd5xp05s4hs?svg=true)](https://ci.appveyor.com/project/chummer5a/chummer5a)
[![Discord](https://img.shields.io/discord/365227581018079232?label=discord)](https://discord.gg/8FKUPjTX2w)
[![License](https://img.shields.io/github/license/chummer5a/chummer5a)](https://www.gnu.org/licenses/gpl-3.0.html)
[![Donations](https://img.shields.io/badge/buy%20me%20a%20coffee-donate-yellow.svg)](https://ko-fi.com/Z8Z7IP4E)

## Project Overview

Chummer is a character creation and management application for the tabletop RPG [Shadowrun, Fifth Edition](https://www.shadowruntabletop.com/products-page/getting-started/shadowrun-fifth-edition).

This repository currently has two tracks:

* **Legacy path**: the WinForms desktop app (`Chummer`) that continues to serve as compatibility reference and regression oracle.
* **Current multi-head runtime (Docker branch)**: API + shared presentation seam + gateway + multi-head UI stack (`Chummer.Blazor`, `Chummer.Avalonia`, `Chummer.Blazor.Desktop`, `Chummer.Avalonia.Browser`, `Chummer.Portal`).

## Current Multi-Head Runtime (Docker Branch)

The `Docker` branch is the current multi-head runtime architecture for this repository:

* `Chummer.Api` is the HTTP host for headless services and workspace routes.
* `Chummer.Application`, `Chummer.Contracts`, `Chummer.Infrastructure`, and `Chummer.Presentation` provide the shared behavior seam.
* `Chummer.Contracts.Rulesets` defines host-neutral ruleset/plugin/script interfaces plus a shared workspace payload envelope for peer SR4/SR5/SR6 module expansion without changing the active runtime seam.
* `/api/session/*` is reserved as the dedicated future session/mobile boundary. Current session routes return explicit `session_not_implemented` receipts until the session product path is implemented, and future session/mobile clients should bind to the dedicated `ISessionClient` seam instead of widening the workbench-oriented `IChummerClient` contract.
* `/api/hub/search` is the first ChummerHub-style read-only discovery surface. It already aggregates RulePacks, RuleProfiles, and runtime locks through one shared browse/query contract so hub-style discovery does not depend on head-specific catalog composition.
* `/api/buildkits/*` exposes a dedicated BuildKit registry seam for starter and career templates. The default registry is intentionally empty until real BuildKit sources are registered, but the public/workbench discovery boundary now exists.
* `/api/rulepacks/*` exposes a dedicated RulePack registry surface, and `/api/profiles/*` exposes curated RuleProfile install targets. Profile preview/apply already route through a dedicated application seam, with apply currently returning explicit deferred receipts until workspace/campaign binding flows land.
* `/api/runtime/profiles/{profileId}` exposes a dedicated runtime-inspector projection for a resolved RuleProfile runtime so support, hub, and future workbench surfaces can inspect fingerprints, pack bindings, warnings, and migration preview data through one shared seam.
* `/api/runtime/locks/*` exposes a reusable runtime-lock catalog derived from current RuleProfiles so saved/published/derived runtime fingerprints have an explicit registry path instead of living only inside profile detail payloads.
* `Chummer.Blazor` is the browser/web head, `Chummer.Avalonia` is the native desktop head, and `Chummer.Blazor.Desktop` is the desktop webview host.
* `Chummer.Portal` is the single public gateway surface and `Chummer.Avalonia.Browser` provides the browser-hosted `/avalonia` route behind the portal profile.
* `Chummer.Web` is retained only as a compatibility/oracle asset and is not part of the default runtime or parity-check contract.
* Default runtime registration currently enables SR5 and SR6 only. `Chummer.Rulesets.Sr4` remains a scaffolded/experimental module and is not part of the default headless/runtime path until import/open/runtime coverage is complete.
* Legacy head policy: `Chummer` and `Chummer.Web` are oracle/parity assets only. Net-new user-facing behavior belongs in the shared seam and active heads; legacy changes must be limited to regression-oracle maintenance, parity extraction, or compatibility verification.
* Runtime compose flows target `chummer-api` and `chummer-blazor`; portal flows add `chummer-portal`, `chummer-blazor-portal`, and `chummer-avalonia-browser`; no `chummer-web` service is part of the active product path.
* Migration execution backlog: [`docs/MIGRATION_BACKLOG.md`](docs/MIGRATION_BACKLOG.md).

## Decommissioned Legacy Runtime Components

The following legacy components are no longer part of the active product/runtime path:

* `Chummer.Web` is no longer part of the default compose/runtime stack and remains only as a compatibility/oracle asset.
* `chummer-web` is no longer an active runtime service or parity-test dependency.
* Static parity extraction from `Chummer.Web/wwwroot/index.html` has been replaced by the checked-in parity oracle at [`docs/PARITY_ORACLE.json`](docs/PARITY_ORACLE.json).
* `Chummer` (WinForms) remains a compatibility reference and regression oracle, not an active multi-head runtime host.

`docker-compose.yml` exposes:

* `chummer-api` (default service)
* `chummer-blazor` (default service)
* `chummer-blazor-portal` (under the `portal` profile; internal `/blazor` path-base host)
* `chummer-avalonia-browser` (under the `portal` profile; internal `/avalonia` browser-head host)
* `chummer-portal` (under the `portal` profile; single landing + proxy gateway)
* `chummer-tests` (under the `test` profile only)

## Running the Docker Branch

The `Docker` branch is validated on Linux with `net10.0` tests through Docker and uses .NET 10 containers.

Start API only:

```bash
docker compose up -d --build chummer-api
```

Start API + Blazor UI:

```bash
docker compose up -d --build chummer-api chummer-blazor
```

Start API + Blazor + Portal landing surface:

```bash
docker compose --profile portal up -d --build chummer-api chummer-blazor-portal chummer-avalonia-browser chummer-portal
```

Direct API access (local/dev/ops or private upstreams):

```bash
export CHUMMER_API_KEY="replace-with-strong-secret"
docker compose up -d --build chummer-api chummer-blazor
```

When set, `Chummer.Api` enforces `X-Api-Key` for non-public `/api/*` routes and both UI heads automatically forward the key.
Set `CHUMMER_PROTECT_API_DOCS=true` to apply the same API-key gate to `/openapi/*` and `/docs/*`.
This is the minimal direct-access fallback for local/dev/ops workflows or private upstream protection. It is not the primary public authentication model.

Hosted/public deployment posture:

* Expose `Chummer.Portal` as the only public origin.
* Keep `Chummer.Api` on a private network behind the portal.
* Use portal cookie auth plus signed portal-owner propagation for hosted/public identity.
* Treat raw `X-Api-Key` mode as local/dev/ops or internal proxy compatibility only.

Owner-scope dev/test bridge:

* `CHUMMER_ALLOW_OWNER_HEADER=true` enables request owner resolution from `X-Chummer-Owner` (override with `CHUMMER_OWNER_HEADER_NAME`) in `Chummer.Api`.
* Authenticated user identity still wins when present; the forwarded owner header path is disabled by default.
* This header seam is for local/test harnesses and portal-edge development only. It is not public authentication, and production/public deployments should use real portal or edge identity instead of trusting forwarded arbitrary owner headers.

Portal-auth owner propagation seam:

* `CHUMMER_PORTAL_OWNER_SHARED_KEY` enables signed portal-to-API owner propagation for authenticated portal requests.
* Configure the same shared key in both `Chummer.Portal` and `Chummer.Api`; the portal strips incoming signed-owner headers and emits fresh signed authenticated owner headers only for `/api`, `/openapi`, and `/docs` proxy traffic.
* `Chummer.Api` prefers this signed portal-owner context ahead of the dev/test `X-Chummer-Owner` bridge when both are present.
* Optional `CHUMMER_PORTAL_OWNER_MAX_AGE_SECONDS` tightens signature freshness validation on the API side (default: `300` seconds).
* This is the authoritative hosted/public bridge for owner-aware requests until full public identity/account management lands.

Portal auth scaffold:

* `Chummer.Portal` now registers cookie authentication and authorization so portal-edge identity can populate `HttpContext.User` before proxying.
* `CHUMMER_PORTAL_DEV_AUTH_ENABLED=true` turns on the minimal dev harness endpoints: `POST /auth/dev-login`, `GET /auth/me`, and `POST /auth/logout`.
* `CHUMMER_PORTAL_REQUIRE_AUTH=true` makes the portal require an authenticated cookie for `/api`, `/openapi`, `/docs`, `/blazor`, and `/avalonia`; the landing page and `/downloads` remain public.
* The dev harness is only a bootstrap path for local/testing and for proving the portal-owner seam. It is not the final public identity system.
* Public deployments should prefer this portal-auth path over direct API exposure; keep `CHUMMER_API_KEY` as a fallback for local/dev/ops or internal service-to-service compatibility only.

Run migration/compliance test loop (branch helper script):

```bash
bash scripts/migration-loop.sh 1
```

Migration loop includes portal surface smoke by default.

```bash
bash scripts/migration-loop.sh 1
```

Optional: disable portal smoke for quick local iterations.

```bash
CHUMMER_PORTAL_E2E=0 bash scripts/migration-loop.sh 1
```

Run the portal surface smoke directly:

```bash
docker compose --profile test --profile portal run --build --rm chummer-playwright-portal
```

Run Linux test profile directly:

```bash
docker compose --profile test run --rm chummer-tests
```

Default endpoints:

* API root: `http://127.0.0.1:8088/`
* API health: `http://127.0.0.1:8088/api/health`
* API content overlays: `http://127.0.0.1:8088/api/content/overlays`
* API OpenAPI: `http://127.0.0.1:8088/openapi/v1.json`
* API docs UI: `http://127.0.0.1:8088/docs/`
* Blazor UI: `http://127.0.0.1:8089/`
* Blazor health: `http://127.0.0.1:8089/health`
* Portal landing (profile `portal`): `http://127.0.0.1:8091/`
* Portal Avalonia route (profile `portal`): `http://127.0.0.1:8091/avalonia/`
* Portal Avalonia health (profile `portal`): `http://127.0.0.1:8091/avalonia/health`
* Portal OpenAPI (profile `portal`): `http://127.0.0.1:8091/openapi/v1.json`
* Portal downloads page (profile `portal`): `http://127.0.0.1:8091/downloads/`
* Portal release manifest (profile `portal`): `http://127.0.0.1:8091/downloads/releases.json`

Portal notes (current milestone):

* `/api`, `/openapi`, and `/docs` are served via in-process portal proxy routing.
* `/api/*`, `/openapi/*`, and `/docs/*` share the same upstream contract through `CHUMMER_PORTAL_API_URL`.
* `/docs` is self-hosted (no external CDN dependency) and loads local assets from the API host.
* `CHUMMER_PROTECT_API_DOCS=true` on the API service protects `/docs` and `/openapi` with the same `X-Api-Key` middleware as protected `/api/*` routes.
* `/blazor` is served through an in-process portal proxy to an internal `chummer-blazor-portal` instance configured with `CHUMMER_BLAZOR_PATH_BASE=/blazor`.
* `/avalonia` is served through an in-process portal proxy to an internal `chummer-avalonia-browser` host service configured with `CHUMMER_AVALONIA_BROWSER_PATH_BASE=/avalonia`.
* Set `CHUMMER_PORTAL_AVALONIA_PROXY_URL` to a different upstream or clear it to fall back to the built-in portal placeholder route.
* `/downloads/` is a local manifest-backed page, `/downloads/releases.json` is sourced from `CHUMMER_PORTAL_RELEASES_FILE` (default `/app/downloads/releases.json`), and `/downloads/<artifact>` serves files from `CHUMMER_PORTAL_RELEASES_DIR` (default `/app/downloads`).
* `CHUMMER_PORTAL_DOWNLOADS_URL` now defaults to `/downloads/` so the landing page stays local-first.
* `CHUMMER_PORTAL_DOWNLOADS_FALLBACK_URL` is optional and only used when local manifest/artifacts are unavailable; when unset, missing `/downloads/*` files return `404` instead of redirect loops.
* Set `CHUMMER_PORTAL_DOWNLOADS_PROXY_URL` to route `/downloads/*` through in-process YARP proxy mode instead of local-file mode.
* `docker-compose.yml` mounts `./Docker/Downloads` into `/app/downloads` for the portal service; sync `desktop-download-bundle` into this folder to make `/downloads` serve real binaries.
* Local sync helper: `bash scripts/runbook.sh downloads-sync <bundleDir> <deployDir>` (defaults: `dist` -> `Docker/Downloads`).
* Portal can forward `X-Api-Key` to API/docs/openapi upstream routes when `CHUMMER_PORTAL_API_KEY` is set (or when `CHUMMER_API_KEY` is present in the portal service environment), but this is intended for internal/private upstream compatibility rather than as the public auth model.
* Portal can also forward signed authenticated owner context to the API/docs/openapi upstream when `CHUMMER_PORTAL_OWNER_SHARED_KEY` is configured on both services; this is the authoritative hosted/public path for owner-aware requests, while `CHUMMER_ALLOW_OWNER_HEADER` remains a disabled-by-default dev/test bridge only.
* Portal cookie-auth scaffolding is always registered; enable `CHUMMER_PORTAL_DEV_AUTH_ENABLED=true` only for local/test login bootstrap and enable `CHUMMER_PORTAL_REQUIRE_AUTH=true` when you want the portal itself to enforce authenticated access to protected upstream routes.
* Non-portal default flows keep `chummer-blazor` at root and do not require path-base configuration.

Cloudflare Tunnel target (portal profile):

* If cloudflared is in another Docker stack, point ingress at the portal host port: `http://host.docker.internal:8091`.
* On Linux, add `extra_hosts: ["host.docker.internal:host-gateway"]` to the cloudflared service if needed.
* If both stacks share an external Docker network, point ingress directly at `http://chummer-portal:8080` instead.
* Keep tunnel ingress as a single origin with catch-all fallback:

```yaml
ingress:
  - hostname: chummer.example.com
    service: http://host.docker.internal:8091
  - service: http_status:404
```

Content overlay notes (`CHUMMER_AMENDS_PATH`):

* `docker-compose.yml` mounts `./Docker/Amends` into `/app/amends` (read-only) and sets `CHUMMER_AMENDS_PATH=/app/amends` for `chummer-api`.
* API startup now enforces content-bundle validation by default (`requireContentBundle: true`) and fails fast if effective content paths do not provide required bundle files such as `lifemodules.xml`.
* Set `CHUMMER_REQUIRE_CONTENT_BUNDLE=true` for other hosts (for example desktop in-process runtime) when you want the same fail-fast content validation behavior.
* Multiple amend roots are supported with platform separators (`:` on Linux/macOS, `;` on Windows) and `,`.
* Active overlay metadata is exposed via `/api/info` (`content.overlays`) and `/api/content/overlays`.
* Overlay manifests accept `mode`:
  `replace-file` (default) keeps exact-name file precedence and powers full-file overrides like `lifemodules.xml`.
  `merge-catalog` applies fragment overlays like `qualities.test-amend.xml` and `en-us.test-amend.xml` onto canonical targets (`qualities.xml`, `en-us.xml`) using deterministic priority order.
* Overlay manifests can include `"checksums"` entries (for example `"data/lifemodules.xml": "sha256:<digest>"`); each listed file is SHA-256 validated during overlay discovery.
* Release/sample amend packs under `Docker/Amends` must include checksum coverage for every `data/*` and `lang/*` payload file; CI enforces this with `scripts/validate-amend-manifests.sh`.
* Sample pack is included at `Docker/Amends/manifest.json` and is configured for `merge-catalog` with test XML content under `Docker/Amends/data` and `Docker/Amends/lang`.

Desktop artifact workflow:

* `.github/workflows/desktop-downloads-matrix.yml` publishes both Avalonia and Blazor desktop artifacts for multiple RIDs and generates `releases.json` with SHA-256 checksums.
* CI manifest generation now uses the shared `scripts/generate-releases-manifest.sh` path to keep local/runbook/workflow output logic in sync.
* Checked-in `Chummer.Portal/downloads/releases.json` remains a local-dev fallback snapshot and is excluded from published portal output; deploy environments must mount/publish real downloads storage and treat published manifest verification as source of truth.
* Local `scripts/generate-releases-manifest.sh` runs now also sync discovered desktop files into `Chummer.Portal/downloads/files` (configurable with `PORTAL_DOWNLOADS_DIR`) so `/downloads/*` can serve generated artifacts without extra manual copy steps.
* The workflow uploads a `desktop-download-bundle` artifact in portal layout (`releases.json` + `files/*`) for direct sync into mounted portal downloads storage.
* Desktop heads default to in-process runtime (`CHUMMER_CLIENT_MODE=inprocess` by default). Set `CHUMMER_CLIENT_MODE=http` only when intentionally running as a thin API client, and provide `CHUMMER_API_BASE_URL` (required) plus `CHUMMER_API_KEY` (optional). Legacy alias `CHUMMER_DESKTOP_CLIENT_MODE` remains supported.
* Push trigger coverage includes shared runtime/presentation layers and portal/download publication paths (`Chummer.Application/**`, `Chummer.Core/**`, `Chummer.Contracts/**`, `Chummer.Desktop.Runtime/**`, `Chummer.Infrastructure/**`, `Chummer.Presentation/**`, `Chummer.Portal/**`, `scripts/generate-releases-manifest.sh`, `scripts/publish-download-bundle.sh`, `scripts/publish-download-bundle-s3.sh`, `scripts/verify-releases-manifest.sh`, `scripts/validate-amend-manifests.sh`) so desktop and download-surface changes run the same artifact pipeline.
* Recommended self-hosted deployment: set repository variable `CHUMMER_PORTAL_DOWNLOADS_DEPLOY_DIR` and use a self-hosted runner that can write directly into mounted portal downloads storage via `scripts/publish-download-bundle.sh`.
* Alternate object-storage deployment: set repository variable `CHUMMER_PORTAL_DOWNLOADS_S3_URI` (plus optional `CHUMMER_PORTAL_DOWNLOADS_S3_LATEST_URI` / `CHUMMER_PORTAL_DOWNLOADS_S3_ENDPOINT_URL`) and credentials secrets (`CHUMMER_PORTAL_DOWNLOADS_AWS_ACCESS_KEY_ID`, `CHUMMER_PORTAL_DOWNLOADS_AWS_SECRET_ACCESS_KEY`) to publish the bundle via `scripts/publish-download-bundle-s3.sh` when the runner cannot write to portal storage directly.
* `scripts/publish-download-bundle.sh` now derives portal file-sync destination from `PORTAL_MANIFEST_PATH` and supports explicit override via `PORTAL_DOWNLOADS_DIR` for non-default portal layouts.
* Manual object-storage sync helper: `bash scripts/runbook.sh downloads-sync-s3 <bundleDir>` (requires `CHUMMER_PORTAL_DOWNLOADS_S3_URI` and `CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL`).
* Deployment mode (`CHUMMER_PORTAL_DOWNLOADS_DEPLOY_ENABLED=true`) enforces published-version verification (`CHUMMER_PORTAL_DOWNLOADS_REQUIRE_PUBLISHED_VERSION=true`) and requires `CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL` so local + live manifests are both validated.
* Deploy job hard-gate: after publish, deployment now verifies `CHUMMER_PORTAL_DOWNLOADS_DEPLOY_DIR/releases.json` contains at least one artifact and fails otherwise.
* `CHUMMER_PORTAL_DOWNLOADS_DEPLOY_DIR` is resolved on the workflow runner filesystem; automatic deployment requires a runner that can write to the portal downloads storage (for example, self-hosted runner with shared mount/network volume).
* Live deployment verification is required: set repository variable `CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL` (portal base URL or direct `.../downloads/releases.json`) so deployment can verify the live portal endpoint after local deploy verification passes.
* Deploy job hard-gate: deployment fails when `CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL` is missing or when the live portal manifest has no published artifacts.
* Deploy verification enforces published manifests (`CHUMMER_PORTAL_DOWNLOADS_REQUIRE_PUBLISHED_VERSION=true`) so `version: "unpublished"` cannot pass deployment gates.
* Deployment jobs now enable per-artifact verification (`CHUMMER_PORTAL_DOWNLOADS_VERIFY_LINKS=true`) so manifest URLs/files are validated, not just manifest shape.
* Canonical topology: self-hosted runner publishes bundle into mounted portal downloads storage (`CHUMMER_PORTAL_DOWNLOADS_DEPLOY_DIR`), then verifies both local manifest file and live `/downloads/releases.json` endpoint before success.
* Treat object storage as the alternate topology, not the default: use it only when shared portal storage is unavailable and keep `/downloads/` proxy verification enabled.
* Example operator configuration: [`docs/examples/self-hosted-downloads.env.example`](docs/examples/self-hosted-downloads.env.example).
* Local verification helper: `bash scripts/runbook.sh downloads-verify <portalBaseOrManifestPath>`.
* Optional local strict artifact check: `DOWNLOADS_VERIFY_LINKS=1 bash scripts/runbook.sh downloads-verify <portalBaseOrManifestPath>`.
* Repo-local smoke helper for downloads sync + verify flow: `RUNBOOK_MODE=downloads-smoke bash scripts/runbook.sh`.
* Parity checklist generator: `RUNBOOK_MODE=parity-checklist bash scripts/runbook.sh` (writes `docs/PARITY_CHECKLIST.md` from `docs/PARITY_ORACLE.json` plus the active catalogs).
* Host readiness probe for strict gates: `RUNBOOK_MODE=host-prereqs bash scripts/runbook.sh`.
* Strict host-side gate wrapper (no soft-skips, defaults to `net10.0`): `bash scripts/runbook-strict-host-gates.sh [optionalTestFilter] [optionalFramework]`.
* Optional unattended path overrides: `RUNBOOK_LOG_DIR` controls runbook log placement and `RUNBOOK_STATE_DIR` controls writable state such as `DOTNET_CLI_HOME`.
* Strict wrapper local stage default excludes API/parity and legacy host-mutating classes (`ApiIntegrationTests`, `DualHeadAcceptanceTests`, `ChummerTest`) so environment-free local checks run first; docker stage still runs full filter scope.
* Strict wrapper now compares tracked `git status` before/after run and fails on new worktree drift unless `STRICT_ALLOW_WORKTREE_DRIFT=1` is explicitly set.
* Manual deployment remains available through workflow dispatch with `deploy_portal_downloads=true`.
* Operator checklist for self-hosted publish/verify and strict host-side test gates: `docs/SELF_HOSTED_DOWNLOADS_RUNBOOK.md`.

## Legacy WinForms Requirements
| Operating System | .NET Framework |
| --- | --- |
| Windows 7 SP1 or 8.1+ | 4.8+ |

## Installation - Windows

Chummer uses a single tree release strategy with two release channels; **Milestone** and **Nightly**.

* **Milestone** releases are a fixed-point for use by living communities and people that prefer not to update their application regularly. These releases are considered to be stable and are recommended for general use. 
* **Nightly** releases are an automated build created with Appveyor at 0000 UTC daily. These releases are more likely to be unstable, but also receive new features and bugfixes faster than the Milestone releases. These are recommended for users that have a specific issue from Milestone that was resolved in Nightly, or are comfortable with testing features. 

1. Download the archive for your preferred update channel [Milestone](https://github.com/chummer5a/chummer5a/releases/latest) or [Nightly](https://github.com/chummer5a/chummer5a/releases) (Select the latest Nightly tag)
2. Extract to preferred folder location. If upgrading, you can extract over the top of an existing folder path.
3. Run Chummer5.exe.

## Installation - Linux and OSX

For the legacy WinForms desktop app, support for other operating systems is limited. For Linux, macOS, and Chrome OS, legacy Chummer can be run through one of three possible ways:

1. Set up and run [Wine](https://www.winehq.org/), an open-source Windows compatibility layer. This is usually not for the faint-of-heart, especially on Chrome OS, but it is completely free. Some details about the steps necessary to run Chummer5a under Wine can be found on [the wiki](https://github.com/chummer5a/chummer5a/wiki#installation). Note that even after you set up Chummer5a to run on Wine, Wine is not perfect and you will encounter some additional bugs while using Chummer5a that you wouldn't run into under Windows.
2. Set up and run [CrossOver](https://www.codeweavers.com/crossover), a hassle-free version of Wine with commercial support. It costs money (though it has a limited free trial), but what you are effectively purchasing is for someone else to do all the hard work setting up Wine for you, no matter what you want to run on it. If you do not want to mess around with technical stuff, we highly recommend using CrossOver.
3. Set up and run a Windows virtual machine through programs like [VirtualBox](https://www.virtualbox.org/), [VMWare Fusion](https://www.vmware.com/products/fusion.html), or [Parallels](https://www.parallels.com/). You will need a valid copy of Windows and lots of disk space, but Chummer5a will run on a Windows virtual machine exactly how it would run under full Windows. Virtual machine hosts are generally not available for Chrome OS, though with some behind-the-scenes tinkering, it can still be possible to run a Windows virtual machine on Chrome OS.

## Contributing

Please take a look at our [contributing](https://github.com/chummer5a/chummer5a/blob/master/CONTRIBUTING.md) guidelines if you're interested in helping!

## History

This project is a continuation of work on the original Chummer projects for Shadowrun 4th and 5th editions, developed by Keith Rudolph and Adam Schmidt. Due to the closure of code.google.com, github repositories of their code have been created as a marker of their work. Please note, Chummer 4 is considered abandonware and is not maintained by the chummer5a team, and exists solely for historical purposes.

* Chummer 4, Keith Rudolph: https://github.com/chummer5a/chummer
* Chummer 5, Keith Rudolph and Adam Schmidt: https://github.com/chummer5a/chummer5

## Sponsors

* [JetBrains](http://www.jetbrains.com/) have been kind enough to provide our development team with licences for their excellent tools:
    * [ReSharper](http://www.jetbrains.com/resharper/)
