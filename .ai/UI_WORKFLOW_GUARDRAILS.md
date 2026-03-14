# 🛡️ UI Workflow Guardrails

**Purpose:** Ensure the mandatory 4-phase UI workflow is NEVER skipped again.

---

## Pre-Flight Checklist (Mandatory Before Any UI Work)

Before writing ANY `.razor` component code, verify:

```markdown
- [ ] Phase 1 COMPLETE: ASCII wireframe exists in `docs/design/<feature>/wireframe.md`
- [ ] Phase 1 APPROVED: User has explicitly approved the wireframe
- [ ] Phase 2 COMPLETE: Mermaid diagrams exist in `docs/design/<feature>/flow.md`
- [ ] Phase 2 APPROVED: User has explicitly approved the flow diagrams
- [ ] Phase 3 READY: Can proceed to `/ui-build`
```

**If ANY checkbox is not ticked → STOP and do Phase 1 or 2 first.**

---

## The 4 Phases (Mandatory Order)

| Phase | Command | Output | Gate |
|-------|---------|--------|------|
| 1 | `/ui-brainstorm` | `docs/design/<feature>/wireframe.md` | User approval |
| 2 | `/ui-flow` | `docs/design/<feature>/flow.md` | User approval |
| 3 | `/ui-build` | Working component code | - |
| 4 | `/ui-review` | Checklist passed | - |

---

## User Enforcement Options

### Option A: Command-Based (Recommended)
You explicitly trigger each phase:
1. "Create wireframe for X" → I do Phase 1
2. "Approve wireframe, do flow" → I do Phase 2
3. "Approved, build it" → I do Phase 3
4. "Review the UI" → I do Phase 4

### Option B: Auto-Block
I automatically check for design docs before any UI code:
- If I detect I'm about to write `.razor` code
- I first check `docs/design/<feature>/`
- If missing → I stop and ask: "Design docs not found. Create wireframe first?"

### Option C: Issue Template
Create GitHub issue template that enforces:
```
## UI Feature Request
- [ ] Phase 1: Wireframe (link to wireframe.md)
- [ ] Phase 2: Flow diagrams (link to flow.md)
- [ ] Phase 3: Implementation
- [ ] Phase 4: Review
```

---

## Current Retroactive Action Needed

### Missing Design Docs

| Page | Wireframe | Flow Diagram |
|------|-----------|--------------|
| Home.razor (Search & Download) | ❌ Missing | ❌ Missing |
| Statistics.razor (Admin) | ❌ Missing | ❌ Missing |

**Create these now?** (Option 3 from your earlier question)

---

## Quick Reference: Skills Location

```
.ai/skills/
├── ui-brainstorm.md    ← Phase 1 rules
├── ui-flow.md          ← Phase 2 rules
├── ui-build.md         ← Phase 3 rules
└── ui-review.md        ← Phase 4 rules
```

---

## Reminder for Locutus

**If user asks for UI/component work:**

1. Check if design docs exist in `docs/design/<feature>/`
2. If NO → Say: "Let's follow the UI workflow. Starting Phase 1..."
3. If YES but not approved → "Wireframe exists but needs your approval. Review?"
4. Only proceed to code after explicit user approval of previous phase

**Never skip phases. Never write code before design approval.**

---

*Which enforcement option do you prefer? A, B, or C?*
*And should I create the retroactive design docs for existing pages now?*
