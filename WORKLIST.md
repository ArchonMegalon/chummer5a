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
| WL-001 | queued | P1 | Push branch `Docker` to origin with latest local commit chain. | user/background | Local branch is ahead; commit-only handoff mode is active. |
| WL-002 | queued | P1 | Run UI Playwright gate (`CHUMMER_UI_PLAYWRIGHT=1`) and capture failures if any. | agent | Currently skipped in migration runbook unless explicitly enabled. |
| WL-003 | queued | P1 | Run Portal Playwright gate (`CHUMMER_PORTAL_PLAYWRIGHT=1`) and capture failures if any. | agent | Currently skipped in migration runbook unless explicitly enabled. |
| WL-004 | queued | P2 | Triage test/build analyzer warning backlog and prioritize zero-risk cleanups. | agent | Focus on recurring MSTEST0037/CA1861/CA1859 warnings first. |
| WL-005 | queued | P2 | Decide whether `scripts/runbook.sh` push mode should remain in repo or be removed/reworked. | agent | Behavior changed to support unattended preapproved flow. |

## Intake Template
Add new items at the bottom:

`| WL-XXX | queued | P1/P2/P3 | <task> | agent/user | <constraints, links, commands> |`
