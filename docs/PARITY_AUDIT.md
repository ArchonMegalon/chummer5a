# Multi-Head Parity Audit

Date: 2026-03-04
Scope: shared behavior parity across `Chummer.Blazor` and `Chummer.Avalonia` over `Chummer.Api` + `Chummer.Presentation`

## Current status

- The Docker branch is now a real multi-head migration path.
- `Chummer.Api` remains a thin host with workspace-focused endpoints.
- `Chummer.Blazor` and `Chummer.Avalonia` both render shared presenter state.
- `Chummer.Web` is treated as a temporary legacy-shell parity artifact, not the target UI path.
- Architecture/compliance tests and Linux docker migration loop are green.

## Primary risk

Execution drift, not architecture shape:

1. The shared presenter seam can regress into a monolith if responsibilities are not split aggressively.
2. Shell/session behavior can drift between heads if command/tab/action enablement is duplicated.
3. Workspace internals are still XML-shaped and need to be pushed to import/export edges.

## Required parity gates

Run:

```bash
bash scripts/migration-loop.sh 1
bash scripts/audit-ui-parity.sh
docker compose --profile test run --rm chummer-tests \
  dotnet test Chummer.Tests/Chummer.Tests.csproj -c Release -f net10.0 \
  --filter "FullyQualifiedName~ArchitectureGuardrailTests|FullyQualifiedName~MigrationComplianceTests|FullyQualifiedName~DualHeadAcceptanceTests"
```

## Migration direction

- Keep one shell contract and one behavior path in `Chummer.Presentation`.
- Keep both heads thin and renderer-oriented.
- Complete parity by tab family via `/api/workspaces/{id}/sections/{sectionId}`.
- Add explicit save/download/export semantics and reduce XML coupling.
- Track all work as issue-sized slices in `docs/MIGRATION_BACKLOG.md`.
