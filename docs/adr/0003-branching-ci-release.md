# ADR-0003: Branching, CI and release workflow

- Status: Accepted
- Date: 2026-10-23

## Context

- LexBank is developed by a single person, with a monorepo containing a backend and, later, a frontend (see ADR-0001).
- `main` must always contain reviewed, working code, and the committee benefits from a traceable history of releases.
- There is no deployment platform at present, so deployment can only be simulated.

## Decision

- **Branches:** work is done on `develop`; `main` is protected and only receives changes through a pull request. No status check is required to merge.
- **CI (`backend.yml`):** runs `dotnet build` and `dotnet test` in Release mode on pull requests and on pushes to `main`, only when backend files, the solution or the workflow itself change. It has a 5-minute timeout.
- **Release (`release.yml`):** when `backend` succeeds on `main`, it creates a git tag and runs a placeholder deployment step. It can also be run manually from `main`.
- **Versioning:** automatic tags are `v0.0.<run_number>`. For significant versions (e.g. `v1.1.0` or `v2.0.0`), a manual run accepts an explicit `v<major>.<minor>.<patch>` tag.
- Each area (backend, later frontend) has its own workflow with its own path filter.

## Alternatives considered

1. **A single workflow for all areas.** Rejected because path filters apply to the whole workflow, so independent triggers per area would need a third-party action and conditions on every job.
2. **Manual tags only.** Rejected as the default because it depends on remembering to tag each merge; it is kept as an option for significant versions.
3. **Requiring the `backend` status check to merge.** Not adopted for now: with path filters, a pull request that does not touch backend files would wait indefinitely for a check that never runs.

## Consequences

- **Positive:** every accepted backend change produces a traceable tag; pipelines per area stay independent and simple.
- **Negative:** merges that do not change backend files produce no release; automatic version numbers do not follow semantic versioning and may skip numbers when a run fails.
- When the frontend exists, `release` will need to decide how it combines both pipelines; as written, each one would trigger its own tag.
- `release.yml` only takes effect once it is present on `main`.
