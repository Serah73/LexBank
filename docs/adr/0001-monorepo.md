# ADR-0001: Use a monorepo for backend and frontend

- Status: Accepted
- Date: 2026-10-23

## Context

- LexBank is a Master's thesis project (UOC) developed by a single person with limited weekly time, so minimizing calendar risk and coordination overhead is a priority.
- It consists of an ASP.NET Core 8 backend and a React + TypeScript frontend. The frontend has not been started yet; the backend is the main focus.
- Changes to the REST API contract affect both the backend and the frontend.
- The repository will be reviewed by an academic committee, so a single traceable history is valuable.

## Decision

Keep the backend and frontend in a single repository, in separate folders (`src/backend`, `src/frontend`), with independent build and test pipelines.

The two parts remain loosely coupled: there is no shared build step, and the only interface between them is the REST API / OpenAPI contract.

## Alternatives considered

1. **Multirepo tied together by a parent repository using git submodules**, to look like a single repository while keeping independent repositories. Rejected because:
   - Submodules are commit pointers: every cross-cutting change requires commits in the child repositories plus a pointer update in the parent, with a risk of detached HEAD or outdated pointers.
   - CI needs recursive checkout and cross-repository credentials.
   - An API contract change becomes several coordinated operations with an inconsistent intermediate state.
   - Its benefits (independent teams, independent lifecycles, code shared across products) do not apply to this project.
2. **Plain multirepo without a parent repository.** Rejected for the same coordination overhead and because it fragments the project history.

## Consequences

- **Positive:** atomic commits and pull requests across backend and frontend; one issue tracker and one history; simpler CI and onboarding.
- **Negative:** backend and frontend share the repository size and history; per-area pipelines must be filtered by path once both exist.
- Revisit this decision only if the frontend or backend needs an independent lifecycle or a different team.
