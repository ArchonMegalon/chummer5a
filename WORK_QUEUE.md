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
| WQ-003 | in_progress | P2 | Continue zero-risk warning reduction from dominant buckets. | agent | 2026-03-05 progress: cleared `CA1875`, `MSTEST0001`, `CS8632`, and `CA1860`, and completed broad `MSTEST0037` assertion modernizations. Latest `docker-tests` run passed (`390/390`) with build stage `1 Warning(s)`, `0 Error(s)`; only residual warning is `AVLN3001` in `Chummer.Avalonia/MainWindow.axaml`. |
| WQ-004 | blocked | P1 | Push `Docker` branch updates to `origin`. | user/background | Commit-only handoff remains active; user can push in background. |
| WQ-005 | done | P2 | Continue `MSTEST0037` reduction batch 2 in top warning files. | agent | Completed 2026-03-05: converted safe numeric/null `Assert.IsTrue` patterns in `ApiIntegrationTests`, `MigrationComplianceTests`, and `CharacterSectionServiceTests`; `docker-tests` passed (`390/390`) and warning baseline moved to `128`. |
| WQ-006 | done | P2 | Continue `MSTEST0037` reduction batch 3 on collection assertions. | agent | Completed 2026-03-05: rewrote high-volume `Assert.AreEqual(0, x.Count)` patterns in compliance/ruleset tests and revalidated. `docker-tests` passed (`390/390`) and warning baseline moved to `122`. |
| WQ-007 | done | P2 | Resolve `CA1860` warnings introduced by `Any()` conversions while keeping `MSTEST0037` gains. | agent | Completed 2026-03-05: replaced `IsFalse(x.Any())` with `Assert.IsEmpty(...)` in touched compliance files and reran `docker-tests`; `CA1860` no longer appears in build warnings. |
| WQ-008 | done | P2 | Continue `MSTEST0037` reduction batch 4 with collection-count modernizations. | agent | Completed 2026-03-05: modernized collection-count assertions and additional boolean/comparison assertions across 30 test files, then revalidated with `bash scripts/runbook.sh docker-tests` (`390/390`). |
| WQ-009 | in_progress | P2 | Resolve remaining `AVLN3001` warning from `Chummer.Avalonia/MainWindow.axaml`. | agent | Inspect Avalonia main-window constructor/resource loading path, apply minimal fix, and rerun `bash scripts/runbook.sh docker-tests` to verify `0 Error(s)` and warning outcome. |

## Intake Template
Append new items at bottom:

`| WQ-XXX | queued | P1/P2/P3 | <task> | agent/user | <next action> |`
