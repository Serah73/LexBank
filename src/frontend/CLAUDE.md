# LexBank frontend

Operational notes for `src/frontend`. The root `CLAUDE.md` still applies (TDD, minimal viable, no git commands, never read `dist/`).

## Commands

Run from `src/frontend`.

- Install: `npm install`
- Tests (single run): `npm test`
- One test: `npx vitest run -t "<test name>"`
- Lint: `npm run lint` (oxlint)
- Type-check and build: `npm run build`
- Development server: `npm run dev` (proxies `/api` to the backend on `http://localhost:5236`, which must be running)

## Current state

- React 19, TypeScript, Vite, Vitest. One screen, in `src/App.tsx`.
- No styles, no router, no state library, no i18n library.
- shadcn/ui, Tailwind CSS and Recharts are the planned stack but are **not installed**. Do not add them, or any other package, until the story that needs them.

## Conventions

- **API codes:** the API returns stable codes, never text. Map every code to its message in `src/messages.ts`; never render a raw code. An unknown error code falls back to `ProcessingFailed`.
- **API responses are untrusted:** check the shape of a response before using it (see `handleFileChange` in `App.tsx`).
- **Language:** user-facing text is in Spanish (the default UI language) and hard-coded for now; English arrives with the multi-language story. Code, comments and test names stay in English.
- **Types:** the interfaces in the frontend mirror the API contract (camelCase JSON, dates as `YYYY-MM-DD` strings). When an endpoint changes, update both sides in the same story.
- **TypeScript settings:** `erasableSyntaxOnly` is on, so use union types of string literals instead of `enum`; `verbatimModuleSyntax` is on, so import types with `import type`.

## Tests

- Vitest with jsdom and React Testing Library (ADR-0005). Tests live next to the code (`App.test.tsx`).
- Replace `fetch` with `vi.stubGlobal('fetch', vi.fn(...))`; tests never call the real backend.
- Test names describe behavior the user can observe (`'shows the stored transactions when the page loads'`). Query the DOM by role or visible text, not by implementation details.
