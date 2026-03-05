# Worklist Queue

Purpose: queue actionable items and run them in order without losing momentum.

## Status Keys
- `queued`
- `in_progress`
- `blocked`
- `done`

## Queue
| ID | Status | Priority | Task | Owner | Notes |
|---|---|---|---|---|---|
| WL-001 | done | P1 | Push branch `Docker` to origin with latest local commit chain. | user/background | Completed 2026-03-05; local `Docker` and `origin/Docker` both resolve to `efa8efa46`. |
| WL-002 | done | P1 | Run UI Playwright gate (`CHUMMER_UI_PLAYWRIGHT=1`) and capture failures if any. | agent | Passed on 2026-03-05 after fixing `scripts/e2e-ui-playwright.cjs` to avoid disabled-tab click flow. |
| WL-003 | done | P1 | Run Portal Playwright gate (`CHUMMER_PORTAL_PLAYWRIGHT=1`) and capture failures if any. | agent | Passed on 2026-03-05. See `/tmp/chummer-portal-e2e.log` (`portal E2E completed`). |
| WL-004 | done | P2 | Triage test/build analyzer warning backlog and prioritize zero-risk cleanups. | agent | Completed 2026-03-05: warning backlog workstream reduced build-stage warnings to `0` (`0 Error(s)`) in `bash scripts/runbook.sh docker-tests`; full suite remains `390/390` passing. |
| WL-005 | done | P2 | Decide whether `scripts/runbook.sh` push mode should remain in repo or be removed/reworked. | agent | Reworked on 2026-03-05: push mode remains but is explicit opt-in (`RUNBOOK_PUSH_ENABLE=1`) and performs a single deterministic `git push <remote> <refspec>` path. |
| WL-006 | done | P1 | Fix UI Playwright flow to avoid clicking disabled `#tab-skills` and re-run `ui-e2e`. | agent | Completed on 2026-03-05; `ui-e2e` now reports `playwright UI flow completed`. |
| WL-007 | done | P2 | Investigate and resolve residual Avalonia compile warning `AVLN3001` in `MainWindow.axaml`. | agent | Completed 2026-03-05 via runtime-loader-compatible `MainWindow` constructor path; docker build stage now reports `0 Warning(s)`. |
| WL-008 | done | P1 | Milestone 1: enforce strict `IChummerClient` shell/bootstrap contract semantics. | agent | Completed 2026-03-05: interface defaults removed; all client stubs now explicitly define shell semantics; full docker gate passed (`390/390`). |
| WL-009 | done | P1 | Milestone 3 platform cleanup: richer bootstrap session snapshot. | agent | Completed 2026-03-05: active workspace session state now flows through shell preferences, bootstrap contracts, API/runtime clients, bootstrap cache, shell presenter initialization, and overview session restore. |
| WL-010 | done | P1 | Milestone 6: eliminate bootstrap workspace-cache preferred-ruleset drift. | agent | Completed 2026-03-05 with cache seeding from explicit shell preferences and regression coverage in `ShellBootstrapDataProviderTests`; docker gate passed (`391/391`). |
| WL-011 | done | P1 | Milestone 4 platform cleanup: neutralize ruleset hosting infrastructure ownership. | agent | Completed 2026-03-05: neutral `Chummer.Rulesets.Hosting` extension now registers generic ruleset infrastructure; SR5 extension is plugin-only; startup paths call both explicitly. |
| WL-012 | done | P1 | Milestone 5 platform cleanup: renderer-facing shell surface normalization. | agent | Completed 2026-03-05: shell surface model now carries last command context and drives active-tab/notice/error/last-command rendering paths across both heads. |
| WL-013 | done | P1 | Milestone 7 platform cleanup: envelope-native workspace persistence internals. | agent | Completed 2026-03-05: `WorkspaceDocument` now carries optional envelope metadata, `FileWorkspaceStore` persists/resolves canonical envelope payload shape, and `WorkspaceService` reads/writes workspace payload through envelope resolution while preserving SR5 `.chum5` import/download behavior. |
| WL-014 | done | P1 | Milestone 8 platform cleanup: operationally truthful download publication guarantees. | agent | Completed 2026-03-05: publish script now enforces deployment-mode live verification + published-version checks, workflow marks deployment mode explicitly, runbook supports unattended deployment-mode sync, and README/compliance checks codify the topology and hard-gates. |
| WL-015 | done | P1 | Milestone 9 platform cleanup: maintain Avalonia shell composition parity. | agent | Completed 2026-03-05: Avalonia now projects shell frame state through `MainWindowShellFrameProjector`, keeps `MainWindow` as composition/apply glue, and dispatches workspace actions via precomputed lookup instead of ad-hoc shell-surface reinterpretation. |
| WL-016 | blocked | P1 | Post-milestone UI/portal regression rerun. | agent | Blocked by sandbox Docker-socket permission errors during portal/ui Playwright container execution in this environment. |
| WL-017 | done | P1 | UI e2e script resilience hardening for non-host-reachable runtimes. | agent | Completed 2026-03-05: added bounded Docker fallback probe controls to `scripts/e2e-ui.sh` and guardrail assertions in compliance tests; host-local validation in this sandbox is limited to script syntax due Docker/`dotnet` constraints. |
| WL-018 | done | P2 | Improve runbook e2e blocker visibility. | agent | Completed 2026-03-05: runbook summary extraction for `ui-e2e` and `portal-e2e` now surfaces Docker daemon permission-denied errors explicitly. |
| WL-019 | done | P1 | Non-CI e2e soft-fail for Docker daemon permission blockers. | agent | Completed 2026-03-05: UI/portal e2e scripts now downgrade daemon-permission-denied failures to explicit skip outside CI, keep CI strict, and surface skip reasons in runbook summaries. |
| WL-020 | done | P1 | Split shell preferences from shell session model. | agent | Completed 2026-03-05: shell preferences/session now persist through separate contracts (`ShellPreferences`, `ShellSessionState`), separate API endpoints (`/api/shell/preferences`, `/api/shell/session`), and separate presenter/client save paths. |
| WL-021 | done | P1 | Move workspace behavior onto ruleset codec seam. | agent | Completed 2026-03-05: introduced ruleset workspace codec contracts/resolver, added SR5 codec implementation, and updated `WorkspaceService` to dispatch import/summary/section/validation/metadata through codec resolution. |
| WL-022 | done | P1 | Persist active tab state in shell session snapshot. | agent | Completed 2026-03-05: active tab now persists via shell session contracts/store/endpoints and is restored through bootstrap + shell presenter initialization. |
| WL-023 | done | P2 | Persist last active tab per workspace. | agent | Completed 2026-03-05: shell session/bootstrap now persist `ActiveTabsByWorkspace`, presenter restores workspace-specific tabs on switch/initialize, and integration/presenter/compliance tests cover the new map flow. |
| WL-024 | done | P1 | Add macOS Intel desktop artifacts to downloads matrix. | agent | Completed 2026-03-05: workflow matrix now publishes both macOS architectures, manifest generation + portal fallback labels include `osx-x64`, and runbook desktop gate enforces the new matrix/manifest coverage. |
| WL-025 | done | P2 | Keep `IChummerClient` interface pure by moving bootstrap helper to extension/helper layer. | agent | Completed 2026-03-05: contract remains method-signature-only and new compliance/runbook guardrails block default shell-bootstrap method bodies from reappearing on the interface. |
| WL-026 | in_progress | P2 | Harden preapproved `runbook local-tests` for unattended sandbox execution. | agent | Add first-run/IPC-safe env defaults and optional no-restore path so targeted tests can run via `bash scripts/runbook.sh` without escalation. |

## Intake Template
Add new items at the bottom:

`| WL-XXX | queued | P1/P2/P3 | <task> | agent/user | <constraints, links, commands> |`
