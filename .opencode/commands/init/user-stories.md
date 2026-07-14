---
description: Read the project description and derive structured, testable user stories at .spec/init/user-stories.md
---
# init:user-stories

You are helping a developer turn a **project description** into **clear, testable user stories**.

## Your goal

Read the existing project description and produce a single document at **`.spec/init/user-stories.md`** that:

1. Identifies every **user type** (persona).
2. Groups functionality into **feature areas**.
3. Writes **user stories** in `As a / I want to / So that` form with **acceptance criteria**.
4. Assigns **priority** to each story.

## Process

### 1. Read the source of truth first

Read **`.spec/init/project-description.md`**. If missing, tell developer to run `init:project-description` first.

### 2. Interview to close gaps

- **Persona boundaries** — who can do what.
- **MVP vs deferred** — which stories are in first version.
- **Priority** — must-have (High) vs nice-to-have (Medium/Low).
- **Acceptance edge cases** — limits, states, failure paths.

### 3. Write the document

```markdown
# <Project Name> — User Stories

<!-- inputs: project-description.md@sha256:<first 12 chars> -->

## Overview

<1–2 paragraphs: what the product is and who it serves.>

**User Types:**
- **<Persona>** - <one-line definition>

---

## 1. <Feature Area>

### US-1.1: <Short Story Title>
**As a** <persona>
**I want to** <capability>
**So that** <benefit>

**Acceptance Criteria:**
- [ ] <specific, testable condition>

**Expected Result:** <the end state when the story is done>

---

## Appendix: User Story Status

| ID | Story | Priority | Status |
|----|-------|----------|--------|
| US-1.1 | <title> | High/Medium/Low | Pending |
```