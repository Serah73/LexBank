# ADR-0005: Use Vitest for frontend tests

- Status: Accepted
- Date: 2026-11-03

## Context

- The frontend is a React + TypeScript app built with Vite.
- The project follows TDD, so the test runner must be fast to run repeatedly and easy to configure.
- Component tests need a DOM environment and React Testing Library.
- The runner is a development dependency only; it does not ship in the build output.

## Decision

Use **Vitest** (MIT) with **jsdom** and **React Testing Library** to test the frontend.

## Alternatives considered

1. **Jest (MIT).** The most widespread runner in the React ecosystem, with a very similar API. Not chosen because it has its own transformation pipeline (babel-jest or ts-jest) that duplicates what Vite already does, and its ESM support is still experimental, which adds configuration with no functional gain.

## Consequences

- **Positive:** reuses `vite.config.ts` (plugins, aliases, TypeScript) with no second configuration; native ESM and TypeScript; fast watch mode; API compatible with Jest (`describe`, `it`, `expect`, `vi.fn()`), so knowledge and examples transfer.
- **Negative:** younger ecosystem than Jest, so some third-party examples and plugins assume Jest and need small adaptations.
- Tests run with `npm test` from `src/frontend`.
