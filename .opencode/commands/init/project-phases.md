---
description: Read the project description, user stories, database schema, and any design specs, then plan the build into numbered, agent-ready phases
---
# init:project-phases

You are helping a developer turn the project spec into a **complete, phased implementation plan**.

## Your goal

Read the existing spec and produce a single document at **`.spec/init/project-phases.md`** that:

1. Breaks the build into **numbered phases** (`Phase 1`, `Phase 5.3`, …).
2. Lists **every task** required to implement everything in the spec.
3. Gives each task concrete **acceptance criteria** and **automated feature tests**.
4. Marks tasks **already completed** as `[x]`.
5. Orders work **foundation-first**, then feature flows.

## Process

### 1. Read the sources of truth first

Read in order:
- **`.spec/init/project-description.md`**
- **`.spec/init/user-stories.md`**
- **`.spec/init/database-schema.md`**
- **`.spec/init/design/`** — if exists, holds UI/design specs.

### 2. Inspect the codebase to detect what is already done

Scan: migrations, models, seeders, frontend components, pages, routes, tests, controllers, services.

### 3. Interview to close gaps

- **Phase priorities / ordering** — which feature flows come first.
- **MVP cut line** — which phases are in first release vs deferred.
- **Ambiguous scope** — flows implied but not fully specified.
- **Design coverage** — screens with no design reference.

## Rules for phasing

### Foundation first, always

1. **Database foundation** — all migrations and lookup-table seeders.
2. **Models & relationships** — every model with all relationships wired up front.
3. **Frontend foundation** — base UI components, layout, shared components.

### Phase sizing (one agent session per phase)

Size each phase so a single AI agent can implement it in **one focused session** — around **10–15 tasks**.

### Numbering

- Top-level: `Phase 1`, `Phase 2`, …
- Sub-phases: `Phase 5.1`, `Phase 5.2`, …

The heading format is a **machine contract** with `scripts/ralph.sh`:

- `## Phase N: <title>` — level-2 headings only.
- Sub-phases at level 3: `### Phase N.M:`.
- Any other level-2 heading ends capture.

### Tasks, tests, and acceptance criteria

- Every task has **acceptance criteria**.
- **Business-logic tasks** must specify **automated feature tests**.
- **Frontend-only tasks** need validatable **acceptance criteria** and a **Design ref**.

## Write the document

````markdown
# <Project Name> — Project Phases

<!-- inputs: project-description.md@sha256:<first 12 chars> user-stories.md@sha256:<first 12 chars> database-schema.md@sha256:<first 12 chars> -->

## Overview

<1–2 paragraphs: the build strategy at a glance.>

**Conventions:**
- `[ ]` pending · `[x]` done in the codebase.
- Phases and sub-phases are numbered for reference by AI agents.

---

## Phase 1: <Foundation — Name>

**Goal:** <one line.> · **Depends on:** <none / Phase N> · **Covers:** <stories/tables/workflows>

### Phase 1.1: <Sub-phase name>

- [ ] **Task:** <what to build>
  - **Acceptance criteria:**
    - <concrete, validatable condition>
  - **Feature tests:** <test name → what business rule it asserts>
  - **Traces:** <US-x.y / table / workflow>

---

## Phase 2: <Name>
...
````