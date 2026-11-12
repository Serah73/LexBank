# ADR-0006: Test the database layer against SQL Server LocalDB

- Status: Accepted
- Date: 2026-11-11

## Context

- Transactions are persisted with EF Core in SQL Server (see `CLAUDE.md`).
- The project follows TDD, and the persistence tests must prove that what is stored is what is read back, including the ordering of the listing and the migration that creates the schema.
- Tests need to run on a developer machine without a manually managed server, and in CI.
- Each test must be isolated from the others and from the development database.

## Decision

Run the database tests (`LexBank.Infrastructure.Tests` and `LexBank.Api.Tests`) against a real **SQL Server LocalDB** instance (`(localdb)\MSSQLLocalDB`), not against EF Core's in-memory provider.

- Each test creates its own uniquely named database and drops it when it finishes.
- The schema is created from the EF Core migrations, so the migrations are exercised by the tests.
- The CI job for the backend runs on `windows-latest`, because LocalDB is only available on Windows.

## Alternatives considered

1. **EF Core in-memory provider.** Fast and needs no installation. Not chosen because it is not a relational database: it does not apply migrations, does not enforce constraints and translates ordering and comparisons differently from SQL Server. Tests could pass while the real behavior fails.
2. **SQLite in memory.** Relational and fast. Not chosen because its SQL dialect and type system differ from SQL Server, so it still would not catch provider-specific problems.
3. **SQL Server in a container (for example Testcontainers or a CI service container).** Same engine as production and portable to Linux runners. Not chosen for now because it requires Docker on every developer machine and more configuration, which goes against the minimal viable principle for a local, family-use project.

## Consequences

- **Positive:** tests run against the same engine as production; migrations are verified; no server to install beyond what Visual Studio and SQL Server Express already provide.
- **Negative:** the tests only run on Windows with LocalDB installed; they are slower than in-memory tests; the CI job runs on a Windows runner, which is slower and consumes more CI minutes than Linux.
- The connection string is currently written in the test code. If tests must run on other platforms, option 3 would replace LocalDB and the connection string would move to an environment variable; that would supersede this ADR.
