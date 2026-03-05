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
| WL-008 | in_progress | P1 | Milestone 1: enforce strict `IChummerClient` shell/bootstrap contract semantics. | agent | Remove interface defaults for shell preference/bootstrap calls, keep behavior parity between HTTP and in-process clients, and validate with tests. |
| WL-009 | queued | P1 | Milestone 2 and 3 platform cleanup: dedicated shell preferences service + richer bootstrap session snapshot. | agent | Implement explicit shell-preference boundary and include active workspace session state in bootstrap contracts. |

## Intake Template
Add new items at the bottom:

`| WL-XXX | queued | P1/P2/P3 | <task> | agent/user | <constraints, links, commands> |`
