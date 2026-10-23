# ADR-0002: Use Clean Architecture for the backend

- Status: Accepted
- Date: 2026-10-23

## Context

- The core of LexBank is the normalization of bank statements into a common schema and a deterministic, keyword-based categorization engine. This logic must not depend on any particular bank format, database or web framework.
- The backend must support several import formats (CSV, Excel, PDF) and persist data with EF Core and SQL Server. These are technical details that may change or grow independently of the business rules.
- The project follows TDD, so the domain and application logic must be testable without a database or a running web server.
- The project is developed by a single person and reviewed by an academic committee, so the structure must be simple to explain and its rules verifiable.

## Decision

Organize the backend in four projects under `src/backend`, following Clean Architecture:

- `LexBank.Domain`: business rules; references nothing.
- `LexBank.Application`: use cases; references Domain only.
- `LexBank.Infrastructure`: persistence and external concerns; references Application only.
- `LexBank.Api`: REST API and composition root; references Application and Infrastructure.

Dependencies point inwards. The rule is enforced through project references, which the compiler checks.

## Alternatives considered

1. **Single project with folders.** Rejected because nothing prevents the domain logic from depending on EF Core or ASP.NET types, so the rule would rely only on discipline.
2. **Traditional N-tier (UI, business logic, data access).** Rejected because the business layer depends on the data access layer, which couples the rules to persistence and makes isolated testing harder.

## Consequences

- **Positive:** the domain and application layers can be tested in isolation; infrastructure details (database, file parsers) can be replaced without touching the rules; the dependency rule is checked at build time.
- **Negative:** more projects and some mapping between layers than a small application strictly needs.
- Dependencies between classes within a layer are not enforced by this structure; if that becomes necessary, architecture tests can be added.
