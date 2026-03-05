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
| WL-001 | blocked | P1 | Push branch `Docker` to origin with latest local commit chain. | user/background | Local branch is ahead; commit-only handoff mode is active. |
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
| WL-014 | in_progress | P1 | Milestone 8 platform cleanup: operationally truthful download publication guarantees. | agent | Enforce deploy/live manifest verification so deployment-enabled runs fail when release manifest remains empty or `unpublished`; keep explicit portal state for no-builds-yet. |
| WL-015 | queued | P1 | Milestone 9 platform cleanup: maintain Avalonia shell composition parity. | agent | Re-audit `MainWindow.axaml.cs` for residual shell logic and keep it as composition glue over shared shell surface/presenter state. |

## Intake Template
Add new items at the bottom:

`| WL-XXX | queued | P1/P2/P3 | <task> | agent/user | <constraints, links, commands> |`
