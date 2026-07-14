---
description: Show the status of the init spec chain (.spec/init/*) and run the next init:* command in the chain
---
# init

You are the router for the init spec chain. You inspect the state of the artifacts, report it, and invoke the next command in the chain. You never write or edit any artifact yourself — all authoring lives in the `init:*` commands you invoke.

## The chain

| # | Artifact | Produced by | Inputs (stamped on line 3) |
|---|---|---|---|
| 1 | `.spec/init/project-description.md` | `/init:project-description` | — (head of chain, no stamp) |
| 2 | `.spec/init/user-stories.md` | `/init:user-stories` | project-description.md |
| 3 | `.spec/init/database-schema.md` | `/init:database-schema` | project-description.md, user-stories.md |
| 4 | `.spec/init/project-phases.md` | `/init:project-phases` | project-description.md, user-stories.md, database-schema.md |
| — | `.spec/init/design/` | developer (manual) | — |

## Flow

### 1 — Presence

```bash
for f in project-description user-stories database-schema project-phases; do
  test -f ".spec/init/$f.md" && echo "present: $f.md" || echo "absent: $f.md"
done
test -d .spec/init/design && echo "present: design/" || echo "absent: design/"
```

### 2 — Freshness

Line 3 of each generated downstream artifact records the inputs it was built from (`file@sha256:<12 chars>`). Recompute and compare:

```bash
for doc in user-stories database-schema project-phases; do
  [ -f ".spec/init/$doc.md" ] || continue
  for pair in $(sed -n '3p' ".spec/init/$doc.md" | grep -oE '[a-z0-9.-]+\.md@sha256:[0-9a-f]{12}'); do
    [ "$(sha256sum ".spec/init/${pair%%@*}" | cut -c1-12)" = "${pair##*:}" ] \
      || echo "stale: $doc.md predates current ${pair%%@*}"
  done
done
```

### 3 — Report

Emit one table:

| Artifact | Status |
|---|---|
| `.spec/init/project-description.md` | `present` / `absent` |
| `.spec/init/user-stories.md` | `present` / `absent` / `stale (<input> changed)` / `present (no stamp)` |
| `.spec/init/database-schema.md` | same |
| `.spec/init/project-phases.md` | same |
| `.spec/init/design/` | `present (manual)` / `absent (optional)` |

### 4 — Next step

Pick exactly one action — first rule that matches wins:

1. **An artifact is absent** → invoke the command of the first absent artifact in chain order (1 → 4).
2. **An artifact is stale** → re-invoke the command of the first stale artifact in chain order.
3. **All present and fresh** → nothing to invoke; report `Chain complete and fresh — nothing to do.`

## Rules

- **Writes nothing itself** — never Write or Edit.
- **One hop per run** — invoke at most one `init:*` command.
- **Never block, never nag** — staleness is a warning, not an error.
- **No git writes** — never stage, commit, or reset.