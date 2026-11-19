# LexBank backend

Operational notes for `src/backend` and `tests/backend`. The root `CLAUDE.md` still applies (dependency rule, TDD, SOLID, no git commands).

## Commands

Run from the repository root.

- Build: `dotnet build`
- All tests: `dotnet test`
- One test project: `dotnet test tests/backend/LexBank.Api.Tests`
- One test: `dotnet test --filter "FullyQualifiedName~<TestName>"`
- Reset the development database: `./reset-db.sh` (stop the API first; it is recreated with the seeded rules on the next start)
- Run the API: `dotnet run --project src/backend/LexBank.Api --launch-profile http` (`http://localhost:5236`, Swagger at `/swagger`)
- Add a migration: `dotnet ef migrations add <Name> --project src/backend/LexBank.Infrastructure --startup-project src/backend/LexBank.Api` (run `dotnet tool restore` once)

## Where things go

- **Domain:** records and enums with no dependencies (`Transaction`, `Category`, `Rule`, `Categorizer`, `FailedRow`, `FailedRowReason`).
- **Application:** one class per use case, named with a verb (`UploadStatement`, method `ExecuteAsync`), plus the interfaces the use cases need (`IStatementReader`, `ITransactionRepository`, `IRuleRepository`).
  `UploadStatement` uses the first `IStatementReader` whose `CanRead(fileName)` is true: a new format is a new reader registered in `Program.cs`.
  `UploadStatement` returns `UploadStatementResult` (including `SkippedTransactions`), not the reader's result: the reader knows nothing about duplicates.
- **Infrastructure:** implementations of those interfaces. Readers locate columns by header name, never by position; a new reader must do the same and reject files where a header is missing. The initial categorization rules are in `InitialRules`, seeded through the migration. Third-party libraries (ClosedXML, EF Core, `Microsoft.Extensions.Identity.Core` for password hashing in `IdentityPasswordHasher`) are used only here.
- **Api:** minimal API endpoints and dependency injection, all in `Program.cs`. Endpoints call a use case or a repository; they hold no business logic. The exceptions a use case throws (`InvalidEmailException`, `EmailAlreadyRegisteredException`...) are mapped to status codes and error codes in `Program.cs`.
  Authentication is a session cookie (ADR-0007): it carries the user's email, and the protected endpoints read it from the `ClaimsPrincipal`. `/api/users` and `/api/session` stay open.

## Conventions

- **Codes, not text:** responses carry stable codes (enum names serialized as strings, error codes such as `UnsupportedStatement`). Never return user-facing messages; the frontend translates them in `src/frontend/src/messages.ts`. A new code needs its translation there.
- **Rules:** priority 1 is the highest. A pattern matches whole words; a `*` at either end relaxes that side. A lone `*` matches everything and is not guarded. Changing `InitialRules` means regenerating `InitialCreate` and running `./reset-db.sh`.
- **Duplicates and category:** `UploadStatement` categorizes the file's transactions before comparing them with the stored ones, because `Transaction` equality includes `Category`. If rules can change after import, compare ignoring the category.
- **Persistence:** `Transaction` has no id property; `Id` is an EF Core shadow property. Do not add persistence concerns to Domain types.
- **Data per user:** `ITransactionRepository` always receives the user's email; `Transaction.UserId` is a shadow property like `Id`. A new endpoint that reads or writes transactions must be protected and must pass the email of the session.
- **Passwords:** never store or log one in clear. `User` overrides `PrintMembers` so its `ToString()` hides the hash; keep it when changing the record.
- **Users table:** `Users.Email` has a unique index; a model change that breaks the development database is fixed by dropping and recreating it.
- **Listing order:** transactions are listed by `Date DESC, Id ASC`.
- **Migrations:** the API applies pending migrations on startup. While the database is for development only, keep a single `InitialCreate`: delete `Migrations/*`, run `dotnet ef migrations add InitialCreate` and drop the database. Once real data must be kept, switch to one migration per model change and never edit an applied one.
- **`Microsoft.EntityFrameworkCore.Design` in `LexBank.Api`:** required by `dotnet-ef` in the startup project (`PrivateAssets=all`). It is not a dependency-rule violation; do not remove it.

## Tests

- xUnit. Test names follow `Method_Scenario_ExpectedResult`.
- Database tests run against real SQL Server LocalDB, never the EF in-memory provider (ADR-0006). Each test creates its own database with a unique name and drops it on dispose; follow `LexBankApiFactory` and `EfTransactionRepositoryTests`.
- API tests use `LexBankApiFactory` (`WebApplicationFactory<Program>`). To control what the reader returns, register `StubStatementReader` with `WithWebHostBuilder`, after `RemoveAll<IStatementReader>()`; otherwise the real reader for that extension wins.
- Tests of a protected endpoint start with `await client.LogInAsync()` (`HttpClientSessionExtensions`), which signs up and logs in; the client keeps the cookie. Pass another email to get a second user.
- Statement fixtures live in `tests/backend/LexBank.Infrastructure.Tests/Fixtures`; the Api tests link the same file instead of copying it.
- `data/` holds real personal bank statements and is git-ignored. Never copy its contents into fixtures, tests, code or documentation.
- LocalDB exists only on Windows; the CI job runs on `windows-latest` for that reason.
