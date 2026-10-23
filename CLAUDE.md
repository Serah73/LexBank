# LexBank

## Context

LexBank is a Master's Thesis (TFM) for the Master's degree in Computer Engineering at UOC, web development area.

It is a web application that imports bank statements exported by Spanish banks (each with a different format), normalizes them into a common bank-independent schema, and categorizes transactions with a deterministic keyword-based rules engine. It deliberately does **not** use AI/ML. Users can correct categories, which also provides data to measure the engine's accuracy.

It is intended for family use, run locally, not as a public service.

## Stack

- Backend: ASP.NET Core 8, EF Core, SQL Server, REST API.
- Frontend (not started): React + TypeScript, shadcn/ui, Tailwind CSS, Recharts.
- Practices: Clean Architecture (Domain, Application, Infrastructure, Api), TDD, ADRs under `docs/adr`, monorepo.

## Expensive decisions

- Multiple users with open email/password sign-up; each user sees only their own data; no roles.
- Categories and rules are common to all users; rules created from corrections join the common set.
- Multi-currency: each transaction is stored and shown in its original currency (EUR by default); no conversion for now.
- Multi-language UI: Spanish and English only.
- Always online; no offline mode.

## Conventions

- **Git:** never commit or run any command that modifies git (add, commit, push, stash, reset, checkout, branch); the user does it manually. Work happens on `develop`; `main` only receives changes through pull requests.
- **Language:** all repository text (code, comments, docs, ADRs, commit messages) is in English. Keep Spanish only where it refers to a Spanish-language origin (proper names, external sources).
- **Dependency rule:** Domain references nothing; Application references Domain; Infrastructure references Application; Api references Application and Infrastructure.
- **Minimal viable:** do not add packages, files, configuration or abstractions that were not requested.

## Commands

Run from the repository root (single solution, `LexBank.sln`):

- `dotnet build`
- `dotnet test`
