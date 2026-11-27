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

- **Domain:** records and enums with no dependencies (`Transaction`, `Category`, `Rule`, `Categorization`, `Categorizer`, `FailedRow`, `FailedRowReason`, `Accuracy`, `BankAccuracy`).
- **Application:** one class per use case, named with a verb (`UploadStatement`, `CorrectCategory`, `CreateRule`, method `ExecuteAsync`), plus the interfaces the use cases need (`IStatementReader`, `ITransactionRepository`, `IRuleRepository`).
  `UploadStatement` uses the first `IStatementReader` whose `CanRead(fileName)` is true: a new format is a new reader registered in `Program.cs`.
  `UploadStatement` returns `UploadStatementResult` (including `SkippedTransactions`), not the reader's result: the reader knows nothing about duplicates.
- **Infrastructure:** implementations of those interfaces. Readers locate columns by header name, never by position; a new reader must do the same and reject files where a header is missing. The initial categorization rules are in `InitialRules`, seeded through the migration. Third-party libraries (ClosedXML, EF Core, `Microsoft.Extensions.Identity.Core` for password hashing in `IdentityPasswordHasher`) are used only here.
- **Api:** minimal API endpoints and dependency injection, all in `Program.cs`. Endpoints call a use case or a repository; they hold no business logic. The exceptions a use case throws (`InvalidEmailException`, `EmailAlreadyRegisteredException`...) are mapped to status codes and error codes in `Program.cs`.
  Authentication is a session cookie (ADR-0007): it carries the user's email, and the protected endpoints read it from the `ClaimsPrincipal`. `/api/users` and `/api/session` stay open.

## Conventions

- **Codes, not text:** responses carry stable codes (enum names serialized as strings, error codes such as `UnsupportedStatement`). Never return user-facing messages; the frontend translates them in `src/frontend/src/messages.ts`. A new code needs its translation there.
- **Rules:** the lower the priority number, the earlier the rule wins. A priority has four digits: type (0 critical, 1 user, 2 strong patterns, 3 generic patterns) and its number inside the type. Seeded rules are type 2 (`InitialRules`: 2000 plus the group number); user rules (`CreateRule`) go downwards from 1999, so the newest wins; types 0 and 3 are unused. A pattern matches whole words; a `*` at either end relaxes that side. `Rule.IsValidPattern` guards what users write (at least one letter or digit, at most 100 characters, no `**` at an end). `CreateRule` trims the pattern before validating, comparing and storing it. A seeded rule's id is its group number times 100 plus its position in the category's list, so append new patterns at the end of the list. Changing `InitialRules` means regenerating `InitialCreate` and running `./reset-db.sh`. `IRuleRepository.ListAsync` orders by priority, then `Id`: the tie-break of the `Categorizer` and the list of rules depend on it. `GET /api/rules` returns `RuleResponse` (pattern, sign, category), not the entity: priority and id stay on the server.
- **Duplicates:** `UploadStatement` compares by an explicit key (date, description, amount, balance, bank), never by record equality: the record also holds `Id`, `Category`, `Rule` and `Corrected`, which change after import. A skipped row is returned as stored, with the user's correction.
- **Corrections:** `Transaction.Category` is the category shown (the user's one when `Corrected`); `Rule` holds the engine's current decision (`null` when no rule matches), apart from the user's choice. `Correct` with the current category returns the transaction unchanged. The engine never overwrites a correction: `ApplyRules` saves with `TryUpdateCategorizationAsync`, which never writes `Corrected` and skips a transaction changed since it was read.
- **Accuracy:** what counts as a hit is defined only in `Accuracy`. A breakdown by category was built and removed on purpose (a correction moves the transaction to another group); do not add it back unless asked. A use case that changes transactions returns the new accuracy in its result.
- **Not found:** a use case signals a missing resource with an exception (`TransactionNotFoundException`), never `null`; the endpoint maps it to `404`. A transaction of another user is "not found".
- **Enums in JSON:** the converter rejects JSON numbers but accepts a number sent as text (`"999"`), so every endpoint that receives an enum must check `Enum.IsDefined`. A request field of enum type must be nullable, otherwise a missing field becomes the first enum value.
- **Rule of a transaction:** `Transaction.Rule` is an `init` property outside the constructor, because EF cannot bind navigations through it. `EfTransactionRepository.AddAsync` replaces it with the stored rule (found by `Id`, because the rule may have changed category since it was read), otherwise EF inserts a copy; `Rule.Id` identifies a rule (the endpoints use it) and has nothing to do with `Priority`; `ListAsync` must `Include` it.
- **Persistence:** `Transaction.Id` is an `init` property outside the constructor and the EF key. Do not add other persistence concerns to Domain types.
- **Data per user:** `ITransactionRepository` always receives the user's email; `Transaction.UserId` is an EF Core shadow property. A new endpoint that reads or writes transactions must be protected and must pass the email of the session.
- **Passwords:** never store or log one in clear. `User` overrides `PrintMembers` so its `ToString()` hides the hash; keep it when changing the record.
- **Users table:** `Users.Email` has a unique index; a model change that breaks the development database is fixed by dropping and recreating it.
- **Listing order:** transactions are listed by `Date DESC, Id ASC`.
- **Migrations:** the API applies pending migrations on startup. While the database is for development only, keep a single `InitialCreate`: run `./reset-migrations.sh`, then `dotnet ef migrations add InitialCreate`, then `./reset-db.sh`. Once real data must be kept, switch to one migration per model change and never edit an applied one.
- **`Microsoft.EntityFrameworkCore.Design` in `LexBank.Api`:** required by `dotnet-ef` in the startup project (`PrivateAssets=all`). It is not a dependency-rule violation; do not remove it.

## Tests

- xUnit. Test names follow `Method_Scenario_ExpectedResult`.
- Database tests run against real SQL Server LocalDB, never the EF in-memory provider (ADR-0006). Each test creates its own database with a unique name and drops it on dispose; follow `LexBankApiFactory` and `EfTransactionRepositoryTests`.
- API tests use `LexBankApiFactory` (`WebApplicationFactory<Program>`), which runs in the `Testing` environment so that databases start empty; ask for `Development` explicitly to test what only exists there (the `a@a.com` account, Swagger). To control what the reader returns, register `StubStatementReader` with `WithWebHostBuilder`, after `RemoveAll<IStatementReader>()`; otherwise the real reader for that extension wins.
- Tests of a protected endpoint start with `await client.LogInAsync()` (`HttpClientSessionExtensions`), which signs up and logs in; the client keeps the cookie. Pass another email to get a second user.
- Statement fixtures live in `tests/backend/LexBank.Infrastructure.Tests/Fixtures`; the Api tests link the same file instead of copying it.
- `data/` holds real personal bank statements and is git-ignored. Never copy its contents into fixtures, tests, code or documentation.
- LocalDB exists only on Windows; the CI job runs on `windows-latest` for that reason.
