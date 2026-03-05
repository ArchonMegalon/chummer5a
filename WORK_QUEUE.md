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
| WQ-002 | in_progress | P1 | Commit CA1861 cleanup once tests are green. | agent | Stage intended test changes plus queue/worklist updates, then commit with a focused message. |
| WQ-003 | queued | P2 | Continue zero-risk warning reduction from dominant buckets. | agent | Target safe test-only cleanups (`MSTEST0037`, `CS8632`) without runtime behavior changes. |
| WQ-004 | blocked | P1 | Push `Docker` branch updates to `origin`. | user/background | Commit-only handoff remains active; user can push in background. |

## Intake Template
Append new items at bottom:

`| WQ-XXX | queued | P1/P2/P3 | <task> | agent/user | <next action> |`
