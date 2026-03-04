# Chummer Web Parity Audit

Date: 2026-03-03
Scope: `Chummer` desktop WinForms vs `Chummer.Web` Linux-native web app

## Current status

- Backend section parsing APIs are broad and test-covered.
- Live docker E2E and unit tests are green for existing coverage.
- UI parity with desktop is partial and currently not close to full feature equivalence.

## Major gaps

1. Desktop workflow parity is incomplete.
   - Character creation/career multi-tab deep interactions and modal selection flows are not fully ported.
2. Menu/tool parity is incomplete.
   - Several desktop commands are missing or mapped to lightweight placeholder behavior.
3. Behavioral parity tests are incomplete.
   - Existing compliance tests mostly verify endpoint/button presence, not full workflow equivalence.

## New audit gate

Run:

```bash
bash scripts/audit-ui-parity.sh
```

This compares a curated desktop command set with web command handlers and fails when required handlers are missing.

## Migration direction

- Add missing command handlers and connect them to real backend workflows.
- Replace placeholder handlers with full UX flows.
- Extend tests from presence checks to behavior-compliance checks.
- Track implementation slices in `docs/MIGRATION_BACKLOG.md`.
