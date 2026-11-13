# LexBank frontend

Single-page application of LexBank, built with React, TypeScript and Vite. It lets the user import a bank statement and review its transactions in the common schema.

For the purpose and scope of the project, see the [main README](../../README.md).

## Current state

The frontend is deliberately minimal at this stage: the thesis prioritizes the backend and the categorization logic, and the visual design is scheduled for the final stories.

- One screen: a file input to import an XLSX statement, the transactions table (date, description, amount, balance) and the list of rows that could not be read.
- When an import skips transactions that were already stored, the screen shows how many were skipped.
- The stored transactions are loaded when the page opens.
- The interface is in Spanish only; English is planned.
- There are no styles yet. shadcn/ui, Tailwind CSS and Recharts are part of the planned stack and are not installed.

## Technology

| Purpose | Tool |
|---|---|
| UI library | React 19 |
| Language | TypeScript |
| Build and development server | Vite |
| Tests | Vitest, React Testing Library and jsdom ([ADR-0005](../../docs/adr/0005-vitest-for-frontend-tests.md)) |
| Linter | oxlint |

## Structure

```
index.html         Entry page
public/            Static assets (application icon)
src/main.tsx       Application bootstrap
src/App.tsx        Main screen
src/App.test.tsx   Tests of the main screen
src/messages.ts    Translation of the API codes into user-facing text
```

## Communication with the API

The frontend calls the backend REST API under `/api`. The development server proxies `/api` to `http://localhost:5236` (see `vite.config.ts`), so the backend must be running to import or list transactions. See the [backend README](../backend/README.md).

The API returns stable codes, never user-facing text. `src/messages.ts` maps each code to the message shown to the user; a code the frontend does not know is shown as a generic error.

## Requirements

- Node.js 24, to run the development tools (the application itself runs in the browser). The version is recorded in `.nvmrc`.

## Commands

Run them from `src/frontend`.

```bash
npm install      # Install dependencies
npm run dev      # Start the development server
npm test         # Run the tests once
npm run lint     # Run the linter
npm run build    # Type-check and build for production into dist/
npm run preview  # Serve the production build locally
```

## Tests

The tests render the application with React Testing Library in a jsdom environment and replace `fetch` with a test double, so they do not need the backend. They follow the project's Test-Driven Development practice: each behavior of the screen is specified by a test written before the code.

## Continuous integration

The `frontend` workflow (`.github/workflows/frontend.yml`) installs the dependencies, runs the linter and the tests, and builds the application on every pull request that changes frontend files. See [ADR-0003](../../docs/adr/0003-branching-ci-release.md).
