---
description: Read the project description and user stories, then derive a suggested database schema in DBML at .spec/init/database-schema.md
---
# init:database-schema

You are helping a developer turn a **project description** and **user stories** into a **suggested database schema** written in **DBML**.

## Your goal

Read the existing project description and user stories, then produce a single document at **`.spec/init/database-schema.md`** that:

1. Models every **entity** as a DBML `Table`.
2. Defines **columns** with concrete types, nullability, defaults, and uniqueness.
3. Declares **relationships** (foreign keys) and **lookup tables**.
4. Follows **conventions of the detected stack**.

## Process

### 1. Read the source of truth first

Read **`.spec/init/project-description.md`** and **`.spec/init/user-stories.md`**. If either file is missing, tell developer to run the missing command first.

### 2. Interview to close gaps

- **Cardinality** — one-to-many vs many-to-many.
- **Ownership & tenancy** — who owns a record.
- **Soft deletes vs hard deletes** — which entities need `deleted_at`.
- **Categorical fields** — every status/type/category → lookup table.
- **Uniqueness & required fields** — which columns are `unique` or `not null`.

### 3. Write the document

````markdown
# <Project Name> — Database Schema

<!-- inputs: project-description.md@sha256:<first 12 chars> user-stories.md@sha256:<first 12 chars> -->

## Overview

<1–2 paragraphs: the data model at a glance.>

## Schema (DBML)

```dbml
// Lookup tables first, then domain tables, then pivots.

Table statuses {
  id bigint [pk, increment]
  name varchar [not null]
  slug varchar [unique, not null]
}

Table users {
  id bigint [pk, increment]
  name varchar [not null]
  email varchar [unique, not null]
  status_id bigint [ref: > statuses.id, not null]
}
```

## Relationships

<Bullet list summarizing each relationship in plain language.>

## Lookup Table Seeds

<For each lookup table, list the initial rows to seed.>

## Notes & Conventions

<Bullets: tables using soft deletes, pivots, indexes, denormalization.>
````

## DB Guidelines

- Table names **plural, snake_case**.
- Every table has `id bigint [pk, increment]`.
- Foreign keys are `<singular>_id` typed `bigint`.
- Include `created_at` and `updated_at timestamp`.
- **No enum DB fields** — always create lookup tables.