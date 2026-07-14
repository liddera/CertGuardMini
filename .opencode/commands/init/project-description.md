---
description: Interview the developer, discover the stack, and produce a structured project description at .spec/init/project-description.md
---
# init:project-description

You are helping a developer turn a rough idea into a **clear, structured project description**.

## Your goal

Produce a single document at **`.spec/init/project-description.md`** that:

1. Sharpens the idea — turns a vague concept into concrete scope.
2. Surfaces **definition gaps** — ambiguities and unmade decisions.
3. Structures the **tech stack** — discovered from the project environment.
4. Extracts the **core concepts** (domain vocabulary).
5. Defines the **core workflows** (main flows the system performs).

## Process

### 1. Discover the environment first

Before asking anything, inspect the project for stack signals:

- Manifests / lockfiles: `composer.json`, `package.json`, `pyproject.toml`, `go.mod`, `Cargo.toml`
- Framework markers: `artisan`, `manage.py`, `next.config.*`, `docker-compose.yml`
- Config/env: `.env.example`, CI files, test runner config
- Existing docs: `README*`, any files under `.spec/`

### 2. Interview to close gaps

Focus on:
- **Purpose & audience** — who uses it, what problem it solves.
- **Scope / MVP boundary** — what is in the first version vs deferred.
- **Core domain concepts** — the nouns and rules.
- **Core workflows** — the main flows, step by step.
- **Constraints** — auth model, integrations, platform, non-goals.

### 3. Write the document

Write to `.spec/init/project-description.md`:

```markdown
# <Project Name> — Project Description

## Overview

<2–4 paragraphs: what it is, who it's for, the core loop/value, the MVP boundary.>

### Key Concepts

- **<Concept>:** <definition, including rules/limits/numbers>
- ...

## Tech Stack

<Table (Layer | Technology) or grouped bullets with real detected versions.>

## Core Workflows

### 1. <Workflow Name>

<Numbered steps or prose. Include request/response examples in fenced code blocks where applicable.>

### 2. <Workflow Name>
...
```

### 4. Self-checks

```bash
F=.spec/init/project-description.md
test -f "$F"
head -1 "$F" | grep -qE '^# .+ — Project Description$'
grep -Fq '## Overview' "$F"
grep -Fq '### Key Concepts' "$F"
grep -Fq '## Tech Stack' "$F"
grep -Fq '## Core Workflows' "$F"
```