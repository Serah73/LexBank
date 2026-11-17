# ADR-0007: Cookie authentication

- Status: Accepted
- Date: 2026-11-16

## Context

- Users sign up with an email and a password, and each user must see only their own data, so the API has to know who is making each request.
- LexBank is run locally for family use and is not published as a public service.
- The frontend and the API share an origin: the development server proxies `/api` to the backend.
- The project avoids packages and code that a story does not need.

## Decision

- **Mechanism:** the session is kept in a cookie issued by the cookie authentication of ASP.NET Core, which is part of the framework. No package is added.
- **Cookie:** `HttpOnly`, so scripts cannot read it, and `SameSite=Strict`, so the browser does not send it on requests that start on another site. It is a session cookie: it lasts until the browser is closed.
- **Identity:** the cookie carries the email of the user. `User` keeps no identifier in Domain; the repositories resolve the user by email.
- **Endpoints:** `POST /api/session` starts the session and `DELETE /api/session` ends it. Wrong credentials are answered with `401` and the code `InvalidCredentials`, whether the email is unknown or the password is wrong, so the response does not reveal which emails are registered.
- **Unauthenticated requests:** the protected endpoints answer `401` instead of redirecting to a login page, which is the default of the framework and is meant for server-rendered sites.
- **Antiforgery tokens are not used.** `SameSite=Strict` already keeps the cookie out of cross-site requests, and tokens would force the frontend to request and send one on every upload. `/api/statements` keeps `.DisableAntiforgery()`.

## Alternatives considered

1. **JSON Web Tokens sent in a header.** Rejected because the frontend would have to store the token where scripts can read it, and revoking a session would need extra code. It pays off when the API serves other origins or clients, which is not the case.
2. **ASP.NET Core Identity as a whole (its user store and its endpoints).** Rejected because it brings its own tables, user model and endpoints, far beyond what the stories need. Only its password hasher is used, in Infrastructure.
3. **Antiforgery tokens on top of `SameSite=Strict`.** Not adopted for now: a second defence against a risk that a local application does not have.
4. **A numeric user identifier exposed in Domain.** Rejected because it would put a persistence concern in a Domain type; the email is already unique.

## Consequences

- **Positive:** no token handling in the frontend, since the browser sends the cookie by itself; no new dependency; sessions end by deleting the cookie.
- **Negative:** the protection against cross-site requests depends on `SameSite=Strict` alone. If the application is ever published, or the frontend and the API stop sharing an origin, antiforgery tokens, `AllowedHosts` and CORS must be reviewed.
- Because the cookie identifies the user by email, letting users change their email would require reissuing the cookie.
- There is no "remember me": the user logs in again after closing the browser.
