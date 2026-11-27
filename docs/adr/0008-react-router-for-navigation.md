# ADR-0008: React Router for navigation

- Status: Accepted
- Date: 2026-11-25
- Note (2026-11-27): the sign-up form later moved to its own page, `/signup`, linked with `/login`. The decision below is unchanged.

## Context

- The frontend is a single screen in `App.tsx`: the sign-up and log-in forms, the statement import and the transactions table share one component.
- Story #17 adds a second page (the rules) and moves the log-in to its own page, with a log-out available on every page. More pages are planned (rule management, charts, accuracy).
- The visual redesign (story #30) will restyle the app. Having the pages and their addresses in place first means the redesign changes components, not how the app is organised.
- The project avoids packages that a story does not need, and `src/frontend/CLAUDE.md` forbids adding one before the story that needs it. This story needs it.

## Decision

- **Library:** `react-router`, the de facto router for React. Each page has its own address, the browser's back and forward buttons work, and a page can be opened or reloaded by its address.
- **Pages:** `/login` (sign-up and log-in forms), `/transactions` and `/rules`. `/` redirects to `/transactions`.
- **Session:** there is no global session state. A protected page that gets `401` from the API redirects to `/login`, in line with ADR-0007 (the API answers `401`, it does not redirect). After logging in the user always goes to `/transactions`.
- **Layout:** the protected pages share a layout with the link between them and the log-out button, which ends the session and goes to `/login`.

## Alternatives considered

1. **A state variable that decides which page to show.** Rejected because pages would have no address, the back button would leave the app and a reload would always return to the first page. It would also have to be replaced once the redesign needs real navigation.
2. **A global session state in `App` that decides between the forms and the pages.** Rejected because it keeps `App` responsible for both the session and the routing, and duplicates what the API already says with `401`.
3. **Other routers (TanStack Router, a hand-made one).** Not adopted: React Router covers the need and is the best known; a hand-made router would be code the project has to maintain for no benefit.

## Consequences

- **Positive:** every page has a stable address; the pages are separated from the shell, so the redesign does not touch routing; a new page is a new route.
- **Negative:** one more dependency, and the existing tests need a router around the component or a fixed address.
- The development server must answer any path with `index.html` (Vite does it by default). If the frontend is ever served by another server, it needs the same fallback.
- A user who opens a page by its address while logged out always lands on `/transactions` after logging in, not on the page they asked for.
