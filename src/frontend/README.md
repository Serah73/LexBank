# LexBank frontend

Single-page application of LexBank, built with React, TypeScript and Vite. It lets the user import a bank statement and review its transactions in the common schema.

For the purpose and scope of the project, see the [main README](../../README.md).

## Current state

The application has a minimalist dark design for desktop screens, built with Tailwind CSS and shadcn/ui ([ADR-0009](../../docs/adr/0009-tailwind-and-shadcn-for-styling.md)). Mobile screens are out of scope.

- Four pages, each with its own address ([ADR-0008](../../docs/adr/0008-react-router-for-navigation.md)): `/login`, `/signup`, `/transactions` and `/rules`. The address `/` redirects to `/transactions`.
- `/login` shows the log-in form (email and password), which starts the session, takes the user to `/transactions` and shows a general message when the credentials are not valid. `/signup` shows the sign-up form, which creates an account and confirms it, with each error from the API shown under its field; it does not start a session. Each of the two pages links to the other. In development, the log-in form is prefilled with the [development account](../backend/README.md#run).
- The pages that need a session (`/transactions` and `/rules`) share a side navigation with the logo, a link to each of them, the current one highlighted, and a button to log out, which ends the session and goes to `/login`.
- Dates and amounts are shown in Spanish format, in euros. Income carries a plus sign and expenses a minus sign, so they do not depend on color alone. Error and success messages carry an icon.
- `/rules` lists every rule, the one that wins first at the top, as a Spanish sentence (the same one shown in the transactions table) with its category. If the rules cannot be loaded, a message is shown.
- `/transactions` shows the import part: a file input to import an XLSX or CSV statement, the transactions table (date, description, amount, balance, bank, category and the rule that assigned it, written as a Spanish sentence) and the list of rows that could not be read.
- Above the table, `/transactions` shows the accuracy of the engine as a whole percentage with the hits and the total (for example "Aciertos del motor: 87 % (174 de 200)"). It is loaded when the page opens and updated from the response of an import, a category correction or "Aplicar reglas". It is not shown when there are no transactions.
- Below that figure, a table titled "Por banco" shows the same percentage, hits and total for each bank that has transactions. It is updated and hidden together with the overall figure.
- The category of each transaction is a selector with every category, "Sin categoría" included. Choosing another one saves it at once and the rule cell then reads "Corregida por ti"; if saving fails, the row keeps its previous category and a message is shown in that panel. After a correction to a category other than "Sin categoría", a "Crear regla" form, in a panel floating at the bottom right that does not block the page, offers to create a rule with an editable pattern taken from the transaction's description (the whole words that fit in 100 characters), the new category and the sign of the amount; the user can confirm or decline, and the errors of the API are shown under the pattern field. When a rule with the same pattern and sign already exists with another category, the form shows its current category and asks whether to replace it with the new one; "Reemplazar" changes the category of that rule and "No reemplazar" goes back to the form to edit the pattern.
- A page that needs a session redirects to `/login` when the API answers its first request with `401`.
- When an import skips transactions that were already stored, the screen shows how many were skipped.
- The "Aplicar reglas" button categorizes the stored transactions again with the current rules, shows how many changed category and reloads the list. A message is shown if the rules cannot be applied or, after applying them, the list cannot be reloaded. These messages are cleared by the next import or category correction.
- The stored transactions are loaded when the page opens.
- The interface is in Spanish only; English is planned.
- Every color is defined once, in `src/index.css` (the logo, an SVG file, carries its own). A light theme is planned; Recharts is part of the planned stack and is not installed.

## Technology

| Purpose | Tool |
|---|---|
| UI library | React 19 |
| Language | TypeScript |
| Build and development server | Vite |
| Navigation | React Router ([ADR-0008](../../docs/adr/0008-react-router-for-navigation.md)) |
| Styles and components | Tailwind CSS, shadcn/ui on Base UI and Lucide icons ([ADR-0009](../../docs/adr/0009-tailwind-and-shadcn-for-styling.md)) |
| Tests | Vitest, React Testing Library and jsdom ([ADR-0005](../../docs/adr/0005-vitest-for-frontend-tests.md)) |
| Linter | oxlint |

## Structure

```
index.html                Entry page
public/                   Static assets (logo and application icon)
src/main.tsx              Application bootstrap
src/App.tsx               Router: the routes of the application
src/Layout.tsx            Side navigation and log-out shared by the pages that need a session
src/LogInPage.tsx         Log-in page
src/SignUpPage.tsx        Sign-up page
src/AuthLayout.tsx        Frame shared by the log-in and sign-up pages
src/TransactionsPage.tsx  Transactions page: import, table and corrections
src/RulesPage.tsx         Rules page: the list of rules
src/SignUpForm.tsx        Sign-up form
src/LogInForm.tsx         Log-in form
src/App.test.tsx          Tests of the log-in and transactions pages
src/RulesPage.test.tsx    Tests of the rules page and the navigation
src/messages.ts           Translation of the API codes into user-facing text
src/format.ts             Spanish format of dates and amounts
src/Message.tsx           Error and success messages
src/index.css             Color palette and base styles
src/components/ui/        shadcn/ui components
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
npm run check    # Run the linter, the tests and the build, in that order
npm run build    # Type-check and build for production into dist/
npm run preview  # Serve the production build locally
```

## Tests

The tests render the application with React Testing Library in a jsdom environment and replace `fetch` with a test double, so they do not need the backend. They follow the project's Test-Driven Development practice: each behavior of the screen is specified by a test written before the code.

## Continuous integration

The `frontend` workflow (`.github/workflows/frontend.yml`) installs the dependencies, runs the linter and the tests, and builds the application on every pull request into `main` that changes frontend files. See [ADR-0003](../../docs/adr/0003-branching-ci-release.md).
