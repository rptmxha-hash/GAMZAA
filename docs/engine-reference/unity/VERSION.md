# Unity — Version Reference

| Field | Value |
|-------|-------|
| **Engine Version** | Unity 6.5 (6000.5.x) |
| **Release Type** | Supported update release (NOT LTS — see note) |
| **Project Pinned** | 2026-07-09 |
| **Last Docs Verified** | 2026-07-09 |
| **LLM Knowledge Cutoff** | ~January 2026 (training coverage effectively ends ~Unity 2023.x / early 6000.x) |
| **Risk Level** | **HIGH** — version is beyond LLM training data |

## Risk Note

Unity 6.5 was released in **June 2026**, beyond the assistant's reliable training
coverage. Agents MUST NOT assume APIs from memory for Unity 6.5-specific behavior —
verify against these reference docs or via WebSearch before suggesting code.

## Post-Cutoff Version Timeline

| Version | Type | Notes |
|---------|------|-------|
| Unity 6.0 | LTS | Supported through ~Oct 2026 (expiring soon) |
| Unity 6.3 | **LTS** | Supported through **Dec 2027** — the long-term-stable alternative |
| Unity 6.5 | Supported | **This project.** Newest features; supported only until the next release |

> **LTS tradeoff**: 6.5 is stable but is a "Supported" update release, not LTS.
> For a multi-year solo project, migrating to a 6.x **LTS** line (e.g. 6.3, or a
> later LTS when available) is worth considering before Production lock. Use
> `/setup-engine upgrade 6.5 <target>` when ready.

## Project-Relevant Highlights (2D mobile idle game)

- **URP is the pipeline** — Built-In Render Pipeline (BIRP) is deprecated in 6.5.
- **Custom 2D lighting & shadow system** — new in 6.5, relevant for atmospheric weather mood.
- **Sprite BlendShape API (Free-Form Deformation, cage-based)** — new in 6.5, useful for
  potato/expression/costume animation without per-frame sprite sheets.
- **On-tile post-processing on all platforms** — enables efficient mobile HDR/color grading.
- **`InstanceID`/`GetInstanceID()` → `EntityId`** — hard error in 6.5. Do not use the old API.

## Maintenance

- Run `/setup-engine refresh` to re-verify these docs against newer Unity releases.
- Run `/setup-engine upgrade 6.5 <new-version>` to migrate the pinned version.

See also: `breaking-changes.md`, `deprecated-apis.md`, `current-best-practices.md`.
