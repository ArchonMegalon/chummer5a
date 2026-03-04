# Agent Memory

Last updated: 2026-03-04

Persistent user preferences:
1. Always continue and never stop as long as there is actionable work to do.
   - Do not pause after milestones, test runs, or pushes.
   - Report progress, then continue automatically.
   - Never wait for confirmation between steps; proceed unless blocked by missing required info, missing credentials/permissions, or an explicit stop request.
2. Prefer pre-approved scripts/command prefixes to avoid permission escalation.
   - Use existing approved scripts and commands by default.
   - If a script can be adjusted to stay within approved execution paths, do that before requesting escalation.
