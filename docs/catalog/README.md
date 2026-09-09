# Docs schema

Package documentation **source of truth** is each project's `docs.json` (for example `Lyo.Net/Data/Compression/Lyo.Compression/docs.json`).

`README.md` next to it is **generated** by `render`. Never edit README as source.

This folder only keeps shared **JSON Schema** for editors/validators.

| Path                                                                 | Role                                                                 |
|----------------------------------------------------------------------|----------------------------------------------------------------------|
| [`schema/package.schema.json`](schema/package.schema.json)           | Package `docs.json` shape                                            |
| [`schema/section.schema.json`](schema/section.schema.json)           | Section union (`paragraph` / `list` / `code` / `table` / `details` / `markdown`) |
| [`templates/package.template.json`](templates/package.template.json) | Starter `docs.json` for new packages                                 |

## Commands

```bash
# Edit docs.json, then regenerate README + portfolio + Blazor
python3 scripts/docs/project-docs.py render
# or from apps/gateway:
npm run sync-docs
```

**Do not hand-edit generated `README.md` files.**
**Do not run `extract`** unless you intentionally want to overwrite `docs.json` from README (`extract --force` only, and it is lossy).
