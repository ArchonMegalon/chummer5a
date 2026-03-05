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
| WL-004 | in_progress | P2 | Triage test/build analyzer warning backlog and prioritize zero-risk cleanups. | agent | 2026-03-05 progress: cleared `CA2263`, `CA1847`, `CS0067`, `CA1859`, `CA1861`, `CA1875`, and `MSTEST0001`; reduced `CS8632` in two nullable-annotations passes. Latest docker-tests run passed (`390/390`) with warning baseline `226` (down from `285`). Dominant bucket is now `MSTEST0037` (~211), with residual `CS8632` (~14), `NU1701` (~15), and `AVLN3001` (~1). |
| WL-005 | done | P2 | Decide whether `scripts/runbook.sh` push mode should remain in repo or be removed/reworked. | agent | Reworked on 2026-03-05: push mode remains but is explicit opt-in (`RUNBOOK_PUSH_ENABLE=1`) and performs a single deterministic `git push <remote> <refspec>` path. |
| WL-006 | done | P1 | Fix UI Playwright flow to avoid clicking disabled `#tab-skills` and re-run `ui-e2e`. | agent | Completed on 2026-03-05; `ui-e2e` now reports `playwright UI flow completed`. |

## Intake Template
Add new items at the bottom:

`| WL-XXX | queued | P1/P2/P3 | <task> | agent/user | <constraints, links, commands> |`
