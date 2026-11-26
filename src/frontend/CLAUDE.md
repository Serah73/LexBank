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

- React 19, TypeScript, Vite, Vitest. React Router (ADR-0008): `src/App.tsx` only holds the routes `/login` (`LogInPage`), `/transactions` (`TransactionsPage`) and `/rules` (`RulesPage`); the last two share `Layout` (links and log-out).
- No styles, no state library, no i18n library.
- shadcn/ui, Tailwind CSS and Recharts are the planned stack but are **not installed**. Do not add them, or any other package, until the story that needs them.

## Conventions

- **API codes:** the API returns stable codes, never text. Map every code to its message in `src/messages.ts`; never render a raw code. An unknown error code falls back to `ProcessingFailed`.
- **Form errors:** show each error under its own field (`aria-invalid` + `aria-describedby`), not as a loose notice; `role="alert"` is only for the generic error. `SignUpFailed`, `LogInFailed`, `CorrectionFailed`, `ApplyRulesFailed`, `ReloadFailed`, `RulesLoadFailed`, `RuleCreationFailed` and `RuleUpdateFailed` exist only in the frontend (network failure or unreadable response), not in the API. `CorrectionFailed` is a general `role="alert"`: the row goes back to its previous category. The log-in error is a general `role="alert"`, not tied to a field, so it does not reveal which of the two was wrong.
- **Session:** there is no session state. A page that needs one redirects to `/login` (`navigate(..., { replace: true })`) when its first request answers `401`; only an explicit `401` does it. After logging in the user always goes to `/transactions`. A test that needs the signed-out view stubs the requests with `401`; the two forms share labels, so query them inside `within(screen.getByRole('form', { name: ... }))`.
- **API responses are untrusted:** check the shape of a response before using it (see `handleFileChange` in `TransactionsPage.tsx`).
- **Language:** user-facing text is in Spanish (the default UI language) and hard-coded for now; English arrives with the multi-language story. Code, comments and test names stay in English.
- **Accuracy:** `TransactionsPage.tsx` takes it from the response of each action that changes transactions, without requesting it again; a new action of that kind must do the same.
- **Rule offer:** `RuleOfferForm` follows the form-error convention (`InvalidRulePattern` and `RuleAlreadyExists` under the pattern field, `RuleCreationFailed` as `role="alert"`). When `RuleAlreadyExists` carries a rule with another category, the form switches to a question to replace it; its failure is `RuleUpdateFailed`. `TransactionsPage.tsx` closes the offer when another correction starts and when an upload starts; leaving the page discards it. The suggested pattern is the description cut to whole words within 100 characters; that limit repeats `Rule.MaxPatternLength` and the `InvalidRulePattern` message, so change them together.
- **Rule sentence:** `describeRule` in `messages.ts` repeats the backend's reading of the `*` wildcards in a pattern; if the pattern syntax changes, change both.
- **Types:** the interfaces in the frontend mirror the API contract (camelCase JSON, dates as `YYYY-MM-DD` strings). When an endpoint changes, update both sides in the same story.
- **TypeScript settings:** `erasableSyntaxOnly` is on, so use union types of string literals instead of `enum`; `verbatimModuleSyntax` is on, so import types with `import type`.

## Tests

- Vitest with jsdom and React Testing Library (ADR-0005). Tests live next to the code (`App.test.tsx`, `RulesPage.test.tsx`), and render `<App />` at the route under test.
- jsdom keeps the URL between tests: set the route with `window.history.pushState({}, '', '/rules')` before `render`, and restore `/` in `afterEach`.
- Replace `fetch` with `vi.stubGlobal('fetch', vi.fn(...))`; tests never call the real backend.
- A `fetch` stub must answer by URL (and method): a queue of answers shared by every request gets consumed by another one, and a test can then pass for the wrong reason.
- Test names describe behavior the user can observe (`'shows the stored transactions when the page loads'`). Query the DOM by role or visible text, not by implementation details.
