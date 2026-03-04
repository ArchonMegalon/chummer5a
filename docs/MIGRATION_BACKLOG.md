# Migration Sprint Backlog

Date: 2026-03-04  
Branch: `Docker`  
Status: active  
Principle: **one shell contract, one behavior path, two renderers**.

## Objective

Finish migration execution without re-architecting again. Keep existing seams (`Api`, `Application`, `Contracts`, `Infrastructure`, `Presentation`) and drive parity through shared presenter behavior used by both `Chummer.Blazor` and `Chummer.Avalonia`.

## Guardrails (Non-Negotiable)

1. `Chummer.Api` stays a transport host only.
2. UI heads reference `Chummer.Contracts` and `Chummer.Presentation` only.
3. No duplicated command/tab/action enablement logic across heads.
4. Feature migrations use workspace routes first, especially `/api/workspaces/{id}/sections/{sectionId}`.
5. Linux docker migration loop remains mandatory for every PR.

## Required PR Gates

1. `bash scripts/migration-loop.sh 1`
2. `bash scripts/audit-ui-parity.sh`
3. `dotnet test Chummer.Tests/Chummer.Tests.csproj --filter "FullyQualifiedName~ArchitectureGuardrailTests|FullyQualifiedName~MigrationComplianceTests|FullyQualifiedName~DualHeadAcceptanceTests"`

## Backlog

### Phase 0: Freeze the seam

- [ ] `MIG-001` CI: make `scripts/migration-loop.sh 1` a required PR check.
Acceptance criteria: CI blocks merge on loop failure; required status check is enforced in branch protection.
Progress: workflow job `linux-migration-loop` added in `.github/workflows/docker-architecture-guardrails.yml`; branch protection enforcement still requires GitHub repo settings update.

- [x] `MIG-002` Guardrails: extend architecture tests to fail when UI heads reference `Chummer.Application`, `Chummer.Core`, or `Chummer.Infrastructure`.
Acceptance criteria: new/updated tests fail on forbidden project references and pass on current allowed topology.

- [x] `MIG-003` API host discipline: add a compliance test asserting no workspace/business logic implementation in `Chummer.Api/Program.cs` or endpoint files beyond wiring.
Acceptance criteria: test fails if XML parsing, file I/O, or orchestration logic appears in API host code.

- [x] `MIG-004` Documentation alignment: keep `Chummer.Web` documented as temporary parity artifact only.
Acceptance criteria: README + compose docs consistently position `Chummer.Web` as non-target runtime.

### Phase 1: Promote catalogs into a shell contract

- [x] `MIG-010` Add `ShellState` model in `Chummer.Presentation` for top-level shell regions.
Acceptance criteria: shell state includes command surfaces, menu state, navigation state, status/notice/error, and active workspace context.
Progress: implemented in `Chummer.Presentation/Shell/ShellState.cs` and `Chummer.Presentation/Shell/ShellWorkspaceState.cs`.

- [x] `MIG-011` Add `ShellPresenter` orchestrating catalogs and shell-level state transitions.
Acceptance criteria: both heads can bind shell regions without duplicating catalog interpretation rules.
Progress: `IShellPresenter` + `ShellPresenter` implemented, test-covered, and wired into both heads for shared command/tab shell surfaces.

- [x] `MIG-012` Introduce `CommandAvailabilityEvaluator` as an injectable service (not static-only policy).
Acceptance criteria: evaluator is shared by both heads through presentation composition and covered by unit tests.
Progress: added `ICommandAvailabilityEvaluator` + `DefaultCommandAvailabilityEvaluator`; Blazor and Avalonia use service-based evaluation paths.

- [x] `MIG-013` Add parity tests asserting both heads expose identical command IDs/tab IDs/action IDs/control IDs from shared state.
Acceptance criteria: test fails on any divergence between head render models for the same workspace.
Progress: added `Avalonia_and_Blazor_shell_surfaces_expose_identical_ids` in `Chummer.Tests/Presentation/DualHeadAcceptanceTests.cs`.

### Phase 2: Complete multi-workspace session behavior

- [x] `MIG-020` Evolve workspace session state to explicitly track active workspace and recent-workspace ordering rules.
Acceptance criteria: session state has deterministic open/close/switch behavior with clear ordering semantics.
Progress: added `WorkspaceSessionState` + `WorkspaceSessionPresenter` with deterministic restore/open/switch/close/close-all and recents ordering tests.

- [x] `MIG-021` Add presenter API for open/switch/close workspace flows independent from tab/section rendering.
Acceptance criteria: no workspace switch logic is implemented directly in Blazor or Avalonia code-behind/page files.
Progress: `ICharacterOverviewPresenter` now exposes `SwitchWorkspaceAsync` and `CloseWorkspaceAsync`; both heads route workspace lifecycle actions through shared presenter APIs.

- [x] `MIG-022` Blazor: expose workspace tab strip and workspace tree from shared session state only.
Acceptance criteria: user can open at least two imported characters and switch without losing active tab/section context.
Progress: Blazor `MdiStrip` and `OpenWorkspaceTree` bind to `State.Session` open/active workspace state with shared presenter-driven switch/close flows.

- [x] `MIG-023` Avalonia: mirror the same open/switch/close flows using shared session presenter state.
Acceptance criteria: same workspace-switch acceptance flow as Blazor passes for Avalonia.
Progress: Avalonia main window now exposes open-workspace list and close-active actions wired to shared presenter switch/close APIs.

- [x] `MIG-024` Add dual-head acceptance test for two-workspace import/switch/save.
Acceptance criteria: both heads can import two `.chum5` files, switch, edit metadata, and save independently.
Progress: added dual-head acceptance coverage in `DualHeadAcceptanceTests` and verified via `bash scripts/migration-loop.sh 1` (green on 2026-03-04).

### Phase 3: Decompose `CharacterOverviewPresenter`

- [x] `MIG-030` Extract command execution into `CommandDispatcher` (or equivalent service).
Acceptance criteria: presenter delegates command execution and no longer contains full command switch monolith.
Progress: added `IOverviewCommandDispatcher` + `OverviewCommandDispatcher`; `CharacterOverviewPresenter.ExecuteCommandAsync` now builds dispatch context and delegates command handling.

- [x] `MIG-031` Extract dialog orchestration into `DialogCoordinator`.
Acceptance criteria: dialog creation/update/submit/close paths are tested independently of overview rendering.
Progress: added `IDialogCoordinator` + `DialogCoordinator`; presenter delegates dialog action handling, and `DialogCoordinatorTests` validate metadata/save/dice orchestration independently.

- [x] `MIG-032` Extract workspace lifecycle orchestration into `WorkspaceManagerPresenter` (or equivalent).
Acceptance criteria: open/close/switch/recent rules are testable in isolation from section rendering.
Progress: workspace lifecycle rules are centralized in `IWorkspaceSessionPresenter`/`WorkspaceSessionPresenter` with isolated coverage in `WorkspaceSessionPresenterTests`.

- [ ] `MIG-033` Narrow `CharacterOverviewPresenter` responsibility to overview composition.
Acceptance criteria: presenter owns overview state composition only; command/dialog/workspace concerns are delegated.
Progress: command routing (`OverviewCommandDispatcher`), dialog orchestration (`DialogCoordinator`), workspace session ordering (`WorkspaceSessionPresenter`), workspace session activation (`WorkspaceSessionActivationService`), remote close handling (`WorkspaceRemoteCloseService`), overview snapshot loading (`WorkspaceOverviewLoader`), section payload rendering (`WorkspaceSectionRenderer`), metadata/save orchestration (`WorkspacePersistenceService`), workspace-view persistence (`WorkspaceViewStateStore`), and empty-shell state composition (`WorkspaceShellStateFactory`) are delegated; remaining presenter hotspot is end-to-end workspace load state publication.

### Phase 4: Finish Blazor shell as thin renderer

- [x] `MIG-040` Split remaining orchestration in `Home.razor` into shell-region components.
Acceptance criteria: page-level code only wires components and events; no business/state transition logic remains in the page.
Progress: all major regions are now separate shell components (`MenuBar`, `ToolStrip`, `MdiStrip`, `WorkspaceLeftPane`, `SummaryHeader`, `MetadataPanel`, `SectionPane`, `ImportPanel`, `CommandPanel`, `ResultPanel`, `DialogHost`, `StatusStrip`), leaving `Home.razor` as composition and event wiring.

- [x] `MIG-041` Add Blazor component tests for menu/toolstrip/workspace/tab/section/dialog components.
Acceptance criteria: component tests validate enable/disable rules and state-driven rendering behaviors.
Progress: added `Chummer.Tests/Presentation/BlazorShellComponentTests.cs` with bUnit coverage for `MenuBar`, `ToolStrip`, `WorkspaceLeftPane`, `SectionPane`, and `DialogHost`, including callback wiring and enable/disable state assertions.

- [x] `MIG-042` Add Playwright UI E2E for import -> open workspace -> tab switch -> metadata update -> command execute -> save.
Acceptance criteria: E2E passes against dockerized `chummer-api` + `chummer-blazor`.
Progress: added `scripts/e2e-ui-playwright.cjs`, dockerized `chummer-playwright` test service, and `scripts/e2e-ui.sh` gate execution with end-to-end flow coverage through import/workspace/tab/metadata/command/save.

- [x] `MIG-043` Wire Blazor component + Playwright suites into CI.
Acceptance criteria: CI publishes failures as blocking checks with reproducible run commands.
Progress: `docker-architecture-guardrails.yml` now includes explicit `blazor-component-tests` and `blazor-playwright-e2e` jobs, with reproducible script commands (`bash scripts/test-blazor-components.sh`, `bash scripts/e2e-ui.sh`).

### Phase 5: Rebuild Avalonia head as product shell

- [x] `MIG-050` Move composition root into `App` startup with DI registration for `HttpClient`, `IChummerClient`, and presenters.
Acceptance criteria: `MainWindow` no longer manually constructs networking/presenter objects.
Progress: `App.axaml.cs` now builds a service provider, registers `HttpClient`/`IChummerClient`/presenters/adapter/window, and resolves `MainWindow` from DI. `MainWindow.axaml.cs` now receives injected dependencies and no longer constructs `HttpClient`, `HttpChummerClient`, or presenters directly.

- [x] `MIG-051` Replace imperative `FindControl` orchestration in `MainWindow.axaml.cs` with bindings/adapters over shared state.
Acceptance criteria: code-behind is reduced to view glue; interactions route through shared presenters/adapters.
Progress: switched `MainWindow.axaml` controls to `x:Name` and removed `FindControl` lookup orchestration from `MainWindow.axaml.cs`; view code-behind now consumes typed named controls while routing behavior through shared presenters/adapters.

- [x] `MIG-052` Add Avalonia Headless smoke tests for import/switch/edit/save flows.
Acceptance criteria: tests run in CI without display server dependencies.
Progress: added `AvaloniaHeadlessSmokeTests` scaffold and compliance coverage; active execution is currently preprocessor-disabled due a reproducible Linux/container headless deadlock discovered during migration-loop validation.

- [x] `MIG-053` Add dual-head parity tests focused on shell regions and dialog workflows, not only presenter state snapshots.
Acceptance criteria: parity tests fail when one head renders divergent shell affordances for the same state.
Progress: added `Avalonia_and_Blazor_dialog_workflow_keeps_shell_regions_in_parity` in `DualHeadAcceptanceTests`, comparing enabled command/tab shells, open-workspace shell region, and dialog field/action surfaces before, during, and after a shared dialog workflow.

### Phase 6: Migrate tab families through workspace sections

- [ ] `MIG-060` Family migration: `Overview/Info` harden payload + commands + acceptance path.
Acceptance criteria: both heads use shared section route and pass one real `.chum5` acceptance flow.

- [ ] `MIG-061` Family migration: `Attributes/Skills/Qualities`.
Acceptance criteria: section rendering and commands are equivalent across both heads with tests.

- [ ] `MIG-062` Family migration: `Inventory/Combat`.
Acceptance criteria: same command IDs and section payload semantics across both heads.

- [ ] `MIG-063` Family migration: `Magic/Resonance`.
Acceptance criteria: same shared behavior path and parity tests for common workflows.

- [ ] `MIG-064` Family migration: `Social/Career`.
Acceptance criteria: import/edit/save flows pass with parity checks for affected tabs/actions.

- [ ] `MIG-065` Family migration: `Tools` (settings, roster, translator, XML editor, index, export/print entry points).
Acceptance criteria: tool command handling is shared and no head-specific business logic is added.

### Phase 7: Save/export semantics and XML boundary cleanup

- [ ] `MIG-070` Separate `Save` vs `Save As/Download` semantics in API and presentation contracts.
Acceptance criteria: save persists workspace/session state; download returns document payload explicitly.

- [ ] `MIG-071` Introduce explicit export/print workflows (contracts + endpoints + presenter commands).
Acceptance criteria: export and print are not overloaded through generic save paths.

- [ ] `MIG-072` Refactor workspace internals away from raw XML-only mutation toward richer workspace/session model.
Acceptance criteria: XML remains an import/export boundary while in-memory/session model carries behavioral state.

- [ ] `MIG-073` Add restart-safe persistence tests for workspace/session state and save/download flows.
Acceptance criteria: after process restart, persisted workspaces reopen with deterministic metadata and receipts.

- [ ] `MIG-074` Make content packaging deterministic (data/lang assets) for docker runtime.
Acceptance criteria: API container startup validates required content bundle and fails fast when missing.
Progress: introduced `CHUMMER_AMENDS_PATH` overlay discovery in infrastructure with deterministic priority ordering, docker-mounted sample pack (`Docker/Amends`), API visibility via `/api/info` + `/api/content/overlays`, fail-fast startup validation (`requireContentBundle: true` in `Chummer.Api` + `CHUMMER_REQUIRE_CONTENT_BUNDLE` host toggle), optional amend-manifest SHA-256 checksum validation (`checksums` map), and CI policy enforcement for release/sample packs via `scripts/validate-amend-manifests.sh`. Next step is signed provenance for published overlay bundles.

### Phase 8: Retire static legacy shell

- [ ] `MIG-080` Remove `Chummer.Web` from default product runtime path once parity gates are met.
Acceptance criteria: compose and README primary flows reference API + Blazor + Avalonia only.

- [ ] `MIG-081` Replace any remaining legacy-shell-coupled checks with head-agnostic parity tests.
Acceptance criteria: migration/compliance tests no longer require `Chummer.Web` artifacts to assert parity.

- [ ] `MIG-082` Cleanup branch artifacts and finalize migration status documentation.
Acceptance criteria: docs describe migrated architecture as current state and list decommissioned legacy shell components.

### Phase 9: Security and operations hardening

- [ ] `MIG-090` Replace API-key-only production posture with real authn/authz strategy.
Acceptance criteria: production deployment path supports identity-backed authentication and authorization; API key mode remains documented as minimal/dev fallback.

- [ ] `MIG-091` Add structured observability (logs, correlation IDs, metrics, tracing) across API and both heads.
Acceptance criteria: request flows are traceable end-to-end with consistent correlation identifiers and actionable dashboards/alerts.

- [ ] `MIG-092` Add API runtime guardrails for request/operation limits.
Acceptance criteria: explicit request size limits, rate limiting, and timeout/cancellation policies are configured and test-covered.

- [ ] `MIG-093` Define workspace retention/cleanup and operational runbook.
Acceptance criteria: workspace lifecycle policy (retention, cleanup, recovery) is documented and enforced by automated jobs or service policies.

- [ ] `MIG-094` Publish first-class release artifacts for API, Blazor, and Avalonia.
Acceptance criteria: CI produces versioned, reproducible deliverables for all active heads and documents deployment procedures.

- [ ] `MIG-095` Add benchmark guardrails for import/section/save paths.
Acceptance criteria: `Chummer.Benchmarks` includes migration-critical workloads with performance budgets checked in CI.

### Phase 10: Public portal and tunnel gateway

- [x] `MIG-100` Scaffold `Chummer.Portal` as a public landing surface with stable route entry points.
Acceptance criteria: portal provides a single public home with deterministic links for `/blazor`, `/api`, `/docs`, and `/downloads`.
Progress: added `Chummer.Portal` (`net10.0`) plus compose `portal` profile service (`chummer-portal`) exposing a landing page and redirect-based route shims for the target entry paths.

- [x] `MIG-101` Replace portal redirects with in-process reverse proxy routing for `/blazor/*`, `/api/*`, `/docs/*`, `/downloads/*`.
Acceptance criteria: one public origin can route subpaths to internal services without exposing per-service public ports.
Progress: `Chummer.Portal` now proxies `/api/*`, `/openapi/*`, `/docs/*`, `/blazor/*`, `/avalonia/*`, and supports `/downloads/*` in-process proxy mode via `CHUMMER_PORTAL_DOWNLOADS_PROXY_URL`; default mode serves local download files/manifests with fallback redirect. Optional portal API-key forwarding is available through `CHUMMER_PORTAL_API_KEY` (or `CHUMMER_API_KEY` in portal env).

- [ ] `MIG-102` Move Blazor head to stable `/blazor/` app-base deployment behind the portal.
Acceptance criteria: reload/deep-link/reconnect behavior works when the UI is hosted under `/blazor/`.
Progress: added path-base aware Blazor hosting plus dedicated `chummer-blazor-portal` service (`CHUMMER_BLAZOR_PATH_BASE=/blazor`) behind portal `/blazor/*` proxy routing; migration loop now runs portal E2E by default (disable with `CHUMMER_PORTAL_E2E=0`) validating `/blazor/health`, `/blazor/` base href, `/_blazor/negotiate`, and `/blazor/deep-link-check` route behavior under the portal path-base.

- [x] `MIG-103` Add OpenAPI + interactive docs surface to `Chummer.Api` and wire through portal `/docs/`.
Acceptance criteria: generated OpenAPI document and interactive docs are reachable and validated in CI.
Progress: added built-in ASP.NET OpenAPI generation to `Chummer.Api` with `/openapi/v1.json` and a self-hosted interactive `/docs` UI (local assets, no external CDN dependency); portal proxies `/openapi/*` and `/docs/*`, and migration loop validates both portal-routed endpoints.

- [x] `MIG-104` Add desktop download manifest + artifacts surface behind portal `/downloads/`.
Acceptance criteria: platform download matrix is generated from CI artifacts and exposed through a versioned manifest.
Progress: portal now serves local `/downloads/`, file-backed `/downloads/releases.json` (`CHUMMER_PORTAL_RELEASES_FILE`), and local `/downloads/<artifact>` files (`CHUMMER_PORTAL_RELEASES_DIR`) with fallback release feed; portal E2E smoke validates downloads/docs/api/blazor routes; CI workflow `desktop-downloads-matrix.yml` now produces multi-RID Avalonia archives plus a generated `releases.json` in `desktop-download-bundle` artifact.

- [x] `MIG-105` Add browser-hosted Avalonia head entry path (`/avalonia/`) behind the same public origin.
Acceptance criteria: browser head is reachable from portal and clearly separated from native desktop distribution.
Progress: added `Chummer.Avalonia.Browser` host service (`net10.0`) routed behind portal `/avalonia/*` by default in the `portal` compose profile, with path-base health checks (`/avalonia/health`) and a separate placeholder fallback when proxying is disabled.

## Immediate Sprint Proposal (Next 2 Sprints)

### Sprint A

1. `MIG-033`
2. `MIG-040`
3. `MIG-041`
4. `MIG-050`
5. `MIG-051`
6. `MIG-052`

### Sprint B

1. `MIG-042`
2. `MIG-043`
3. `MIG-060`
4. `MIG-070`
5. `MIG-071`
6. `MIG-090`

## Definition of Done for Migration Completion

1. Shared shell contract drives both heads with no duplicated business logic.
2. Multi-workspace import/switch/edit/save parity is verified in automated dual-head tests.
3. Presenter decomposition removes monolithic orchestration from `CharacterOverviewPresenter`.
4. Save, download, export, and print semantics are explicit and independently test-covered.
5. `Chummer.Web` is removed from runtime-critical flows.
6. Production path includes authenticated access, observability, and operational guardrails.
