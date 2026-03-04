# Agent Runtime Instructions

## Persistent Memory
- Always load and follow `AGENT_MEMORY.md` before starting work.
- Treat entries in `AGENT_MEMORY.md` as active user preferences until the user changes them.

## Execution Rule
- Never stop at milestones while actionable work remains.
- Continue automatically after each completed patch/milestone and report what was reached.
- Only pause when blocked by missing required information, missing credentials/permissions, or an explicit user stop request.
