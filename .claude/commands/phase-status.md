Read the current project state and give me a concise status briefing.

1. Read `docs/project-status.md` — current phase, overall progress count, blockers, decisions.
2. Read `docs/phase-tracker.md` — find the current active phase section and list: checked tasks (done), unchecked tasks (remaining), and Definition of Done items.
3. Read `docs/changelog.md` — last 5 entries under any section.

Then output a briefing in this format:

---
**Current Phase:** <phase name and number>
**Progress:** <X done / Y total tasks in this phase>

**Completed this phase:**
- bullet list of checked tasks

**Remaining:**
- bullet list of unchecked tasks (grouped: Backend / Frontend / Infrastructure)

**Definition of Done — status:**
- each DoD item with ✅ or ⬜

**Blockers:** <none or list>

**Last changes (changelog):**
- last 5 bullets verbatim

**Recommended next task:** <single most logical next task to start>
---

Keep it tight — no padding, no commentary outside the template.
