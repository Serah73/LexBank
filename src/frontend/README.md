# LexBank frontend

Single-page application of LexBank, built with React, TypeScript and Vite. It lets the user import a bank statement and review its transactions in the common schema.

For the purpose and scope of the project, see the [main README](../../README.md).

## Current state

The frontend is deliberately minimal at this stage: the thesis prioritizes the backend and the categorization logic, and the visual design is scheduled for the final stories.

- Three pages, each with its own address ([ADR-0008](../../docs/adr/0008-react-router-for-navigation.md)): `/login`, `/transactions` and `/rules`. The address `/` redirects to `/transactions`.
- `/login` shows the sign-up form and the log-in form (email and password). The sign-up form creates an account and confirms it, with each error from the API shown under its field; it does not start a session. The log-in form starts the session, takes the user to `/transactions` and shows a general message when the credentials are not valid.
- The pages that need a session (`/transactions` and `/rules`) share a bar with a link to each of them and a button to log out, which ends the session and goes to `/login`.
- `/rules` lists every rule, the one that wins first at the top, as a Spanish sentence (the same one shown in the transactions table) with its category. If the rules cannot be loaded, a message is shown.
- `/transactions` shows the import part: a file input to import an XLSX or CSV statement, the transactions table (date, description, amount, balance, bank, category and the rule that assigned it, written as a Spanish sentence) and the list of rows that could not be read.
- The category of each transaction is a selector with every category, "Sin categoría" included. Choosing another one saves it at once and the rule cell then reads "Corregida por ti"; if saving fails, the row keeps its previous category and a message is shown. After a correction to a category other than "Sin categoría", a "Crear regla" form below the table offers to create a rule with the transaction's description as an editable pattern, the new category and the sign of the amount; the user can confirm or decline, and the errors of the API are shown under the pattern field. When a rule with the same pattern and sign already exists with another category, the form shows its current category and asks whether to replace it with the new one; "Reemplazar" changes the category of that rule and "No reemplazar" goes back to the form to edit the pattern.
- A page that needs a session redirects to `/login` when the API answers its first request with `401`.
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
| Navigation | React Router ([ADR-0008](../../docs/adr/0008-react-router-for-navigation.md)) |
| Tests | Vitest, React Testing Library and jsdom ([ADR-0005](../../docs/adr/0005-vitest-for-frontend-tests.md)) |
| Linter | oxlint |

## Structure

```
index.html                Entry page
public/                   Static assets (application icon)
src/main.tsx              Application bootstrap
src/App.tsx               Router: the routes of the application
src/Layout.tsx            Links and log-out shared by the pages that need a session
src/LogInPage.tsx         Log-in page (sign-up and log-in forms)
src/TransactionsPage.tsx  Transactions page: import, table and corrections
src/RulesPage.tsx         Rules page: the list of rules
src/SignUpForm.tsx        Sign-up form
src/LogInForm.tsx         Log-in form
src/App.test.tsx          Tests of the log-in and transactions pages
src/RulesPage.test.tsx    Tests of the rules page and the navigation
src/messages.ts           Translation of the API codes into user-facing text
```

## Communication with the API

The frontend calls the backend REST API under `/api`. The development server proxies `/api` to `http://localhost:5236` (see `vite.config.ts`), so the backend must be running to sign up, log in, import or list transactions. The session is a cookie that the browser sends by itself. See the [backend README](../backend/README.md).

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

The `frontend` workflow (`.github/workflows/frontend.yml`) installs the dependencies, runs the linter and the tests, and builds the application on every pull request into `main` that changes frontend files. See [ADR-0003](../../docs/adr/0003-branching-ci-release.md).
