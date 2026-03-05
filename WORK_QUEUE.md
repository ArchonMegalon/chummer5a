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
| WQ-003 | in_progress | P2 | Continue zero-risk warning reduction from dominant buckets. | agent | 2026-03-05 progress: cleared `CA1875` and `MSTEST0001`, fully eliminated `CS8632`, and reduced `MSTEST0037` in several assertion-conversion batches. Latest `docker-tests` run passed (`390/390`) with `128 Warning(s)`, `0 Error(s)`; dominant residual bucket is `MSTEST0037`. |
| WQ-004 | blocked | P1 | Push `Docker` branch updates to `origin`. | user/background | Commit-only handoff remains active; user can push in background. |
| WQ-005 | done | P2 | Continue `MSTEST0037` reduction batch 2 in top warning files. | agent | Completed 2026-03-05: converted safe numeric/null `Assert.IsTrue` patterns in `ApiIntegrationTests`, `MigrationComplianceTests`, and `CharacterSectionServiceTests`; `docker-tests` passed (`390/390`) and warning baseline moved to `128`. |
| WQ-006 | queued | P2 | Continue `MSTEST0037` reduction batch 3 on collection assertions. | agent | Target safe `Assert.AreEqual(0, list.Count)` and related patterns (`IsEmpty`/`IsNotEmpty`/`HasCount`) in `CharacterSectionServiceTests`, `ContentOverlayCatalogServiceTests`, and `BlazorShellComponentTests`, then run `bash scripts/runbook.sh docker-tests`. |

## Intake Template
Append new items at bottom:

`| WQ-XXX | queued | P1/P2/P3 | <task> | agent/user | <next action> |`
