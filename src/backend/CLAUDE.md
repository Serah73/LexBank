# LexBank backend

Operational notes for `src/backend` and `tests/backend`. The root `CLAUDE.md` still applies (dependency rule, TDD, SOLID, no git commands).

## Commands

Run from the repository root.

- Build: `dotnet build`
- All tests: `dotnet test`
- One test project: `dotnet test tests/backend/LexBank.Api.Tests`
- One test: `dotnet test --filter "FullyQualifiedName~<TestName>"`
- Run the API: `dotnet run --project src/backend/LexBank.Api --launch-profile http` (`http://localhost:5236`, Swagger at `/swagger`)
- Add a migration: `dotnet ef migrations add <Name> --project src/backend/LexBank.Infrastructure --startup-project src/backend/LexBank.Api` (run `dotnet tool restore` once)

## Where things go

- **Domain:** records and enums with no dependencies (`Transaction`, `FailedRow`, `FailedRowReason`).
- **Application:** one class per use case, named with a verb (`UploadStatement`, method `ExecuteAsync`), plus the interfaces the use cases need (`IStatementReader`, `ITransactionRepository`).
- **Infrastructure:** implementations of those interfaces. Third-party libraries (ClosedXML, EF Core) are used only here.
- **Api:** minimal API endpoints and dependency injection, all in `Program.cs`. Endpoints call a use case or a repository; they hold no business logic.

## Conventions

- **Codes, not text:** responses carry stable codes (enum names serialized as strings, error codes such as `NotRuralviaStatement`). Never return user-facing messages; the frontend translates them in `src/frontend/src/messages.ts`. A new code needs its translation there.
- **Persistence:** `Transaction` has no id property; `Id` is an EF Core shadow property. Do not add persistence concerns to Domain types.
- **Listing order:** transactions are listed by `Date DESC, Id ASC`.
- **Migrations:** the API applies pending migrations on startup. Every model change needs a migration; never edit an applied one.
- **`Microsoft.EntityFrameworkCore.Design` in `LexBank.Api`:** required by `dotnet-ef` in the startup project (`PrivateAssets=all`). It is not a dependency-rule violation; do not remove it.

## Tests

- xUnit. Test names follow `Method_Scenario_ExpectedResult`.
- Database tests run against real SQL Server LocalDB, never the EF in-memory provider (ADR-0006). Each test creates its own database with a unique name and drops it on dispose; follow `LexBankApiFactory` and `EfTransactionRepositoryTests`.
- API tests use `LexBankApiFactory` (`WebApplicationFactory<Program>`).
- Statement fixtures live in `tests/backend/LexBank.Infrastructure.Tests/Fixtures`; the Api tests link the same file instead of copying it.
- `data/` holds real personal bank statements and is git-ignored. Never copy its contents into fixtures, tests, code or documentation.
- LocalDB exists only on Windows; the CI job runs on `windows-latest` for that reason.
