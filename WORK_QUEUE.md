# Work Queue

Purpose: keep a live, ordered queue of actionable work and execute items continuously.

## Status Keys
- `queued`
- `in_progress`
- `blocked`
- `done`

## Queue
| ID | Status | Priority | Task | Owner | Next Action |
|---|---|---|---|---|---|
| WQ-001 | done | P1 | Finish current CA1861 cleanup slice in tests. | agent | Completed 2026-03-05: `bash scripts/runbook.sh docker-tests` passed (`390/390`), `CA1861` count is `0`, and warning baseline moved to `410`. |
| WQ-002 | done | P1 | Commit CA1861 cleanup once tests are green. | agent | Completed 2026-03-05 in commit `2b60aedb1` (`chore: add work queue and clear CA1861 test warnings`). |
| WQ-003 | in_progress | P2 | Continue zero-risk warning reduction from dominant buckets. | agent | 2026-03-05 progress: cleared `CA1875` and `MSTEST0001`; latest `docker-tests` run passed (`390/390`) with `394 Warning(s)`, `0 Error(s)`. |
| WQ-004 | blocked | P1 | Push `Docker` branch updates to `origin`. | user/background | Commit-only handoff remains active; user can push in background. |

## Intake Template
Append new items at bottom:

`| WQ-XXX | queued | P1/P2/P3 | <task> | agent/user | <next action> |`
