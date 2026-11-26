# LexBank backend

REST API of LexBank, built with ASP.NET Core 8, EF Core and SQL Server. It imports bank statements, normalizes them into the common transaction schema and persists them.

For the purpose and scope of the project, see the [main README](../../README.md).

## Architecture

The backend follows Clean Architecture ([ADR-0002](../../docs/adr/0002-clean-architecture.md)), with one project per layer:

| Project | Responsibility | References |
|---|---|---|
| `LexBank.Domain` | Business concepts: `Transaction`, `Bank`, `Category`, `AmountSign`, `Rule`, `Categorization`, `Categorizer`, `FailedRow`, `FailedRowReason`, `User`. | Nothing |
| `LexBank.Application` | Use cases (`UploadStatement`, `CorrectCategory`, `CreateRule`, `ChangeRuleCategory`, `ApplyRules`, `SignUp`, `LogIn`) and the interfaces they need (`IStatementReader`, `ITransactionRepository`, `IRuleRepository`, `IUserRepository`, `IPasswordHasher`). | Domain |
| `LexBank.Infrastructure` | Implementations: `RuralviaXlsxReader` (ClosedXML), `RevolutCsvReader`, `EfTransactionRepository`, `EfRuleRepository`, `EfUserRepository` and `LexBankDbContext` (EF Core), `InitialRules` (the seeded rules), `IdentityPasswordHasher` (password hashing with `Microsoft.Extensions.Identity.Core`), migrations. | Application |
| `LexBank.Api` | Minimal API endpoints and dependency injection. | Application, Infrastructure |

## API

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/statements` | Requires a session. Imports a statement sent as `multipart/form-data` in the `file` field. Returns the transactions read from the file as they are stored (a skipped transaction comes back with the category the user chose, if any), the rows that could not be read and `skippedTransactions` (the number of transactions not stored because they were already imported). Stores only the new valid transactions, for the user of the session. |
| `GET` | `/api/transactions` | Requires a session. Returns the stored transactions of the user of the session, newest first. |
| `PUT` | `/api/transactions/{id}/category` | Requires a session. Changes the category of one of the user's transactions from a JSON body `{ "category": "Restaurants" }` and returns the updated transaction with `200 OK`. |
| `POST` | `/api/transactions/apply-rules` | Requires a session. Categorizes the stored transactions of the user again with the current rules, without a body, and returns `200 OK` with `{ "changed" }`, the number of transactions whose category changed. A corrected transaction keeps its category, and a transaction corrected while the rules are being applied is left as the user set it. |
| `GET` | `/api/rules` | Requires a session. Returns every rule (`pattern`, `sign` and `category`), seeded or not, ordered so that the one that wins first comes first. The priority and the id are not part of the response. |
| `POST` | `/api/rules` | Requires a session. Creates a categorization rule from a JSON body `{ "pattern", "category", "sign" }` and returns it with `201 Created`, with its priority. The rule joins the common set, so every user's later imports use it; transactions already stored are not changed. |
| `PUT` | `/api/rules/{id}/category` | Requires a session. Changes the category of a rule, seeded or not, from a JSON body `{ "category": "Health" }` and returns the updated rule with `200 OK`. Its pattern, sign and priority do not change; transactions already stored are not changed. |
| `POST` | `/api/users` | Creates a user account from a JSON body `{ "email", "password" }`. Returns `201 Created`. It does not start a session. A missing field is treated as empty. |
| `POST` | `/api/session` | Logs in with a JSON body `{ "email", "password" }`. Returns `200 OK` and sets the session cookie. A missing field is treated as empty. |
| `DELETE` | `/api/session` | Ends the session and removes the cookie. Returns `204 No Content`. |

The API returns stable codes instead of user-facing text; the frontend translates them. Enumerations are serialized as strings.

- A row that cannot be read is reported with its row number and a reason: `InvalidDate`, `InvalidAmount` or `InvalidBalance`.
- A file that is not a supported statement is rejected with `400 Bad Request` and the error code `UnsupportedStatement`. The reader is chosen by the file extension: `.xlsx` is read as a Ruralvía statement and `.csv` as a Revolut statement. The Revolut reader keeps only the completed transactions of the current account (`Actual`) and stores each amount minus its fee; a row with fewer fields than the header makes the whole file unsupported. The Ruralvía reader looks for the header row in the first 10 rows of the sheet, finds each column by its header name (their order does not matter) and ignores everything above the header; if the header is not found or a column is missing, the file is unsupported.
- A sign-up is rejected with `400 Bad Request` and the error code `InvalidEmail` or `InvalidPassword`, or with `409 Conflict` and `EmailAlreadyRegistered`. If both fields are invalid, only `InvalidEmail` is returned.
- A log-in with an unknown email or a wrong password is rejected with `401 Unauthorized` and the error code `InvalidCredentials`; the response is the same in both cases, so it does not reveal which emails are registered.
- A category correction for a transaction that does not exist, or that belongs to another user, is answered with `404 Not Found` and no body; the transaction is not changed. A missing, unknown or numeric category is answered with `400 Bad Request`.
- A rule is rejected with `400 Bad Request` and the error code `InvalidRulePattern` when its pattern has no letter or digit, has more than 100 characters or has `**` at either end, and with `409 Conflict` and `RuleAlreadyExists` when a rule with the same pattern (ignoring case) and the same sign already exists, seeded or not, whatever its category; that response also carries the existing rule (`rule`, with its `id`). A missing category or sign, the category `Uncategorized` or an unknown value is answered with `400 Bad Request` and no code. A category change for a rule that does not exist is answered with `404 Not Found` and no body, and a missing, unknown or `Uncategorized` category with `400 Bad Request`.
- A request to a protected endpoint without a valid session is answered with `401 Unauthorized` and no body. A session whose user no longer exists is rejected the same way.

Authentication uses a session cookie (`HttpOnly`, `SameSite=Strict`) that carries the email of the user; see [ADR-0007](../../docs/adr/0007-cookie-authentication.md).

An email is valid when it has at least one character before and after the first `@` and at most 256 characters. A password must have between 8 and 128 characters; there are no composition rules. Emails are compared without distinguishing case (the collation of SQL Server) and are stored as written. Passwords are stored only as a hash.

Each transaction carries the bank it was read from as a stable code (`bank`: `Ruralvia` or `Revolut`), stored as text. A transaction is considered already imported when a transaction stored for the same user has the same date, description, amount, balance and bank; its category and rule do not take part in the comparison, so a transaction whose category the user corrected is still recognized. Identical rows inside the same file are kept: only as many copies as are already stored are skipped.

Each transaction also carries its category as a stable code (`category`, for example `Groceries`; `Uncategorized` when no rule matches) and the rule that decided it (`rule`, with `id`, `pattern`, `category`, `sign` and `priority`), which is `null` when no rule matched. Each transaction also has an `id` and a `corrected` flag. When the user corrects the category, `category` holds the user's choice, `corrected` is `true` and the correction does not change `rule`, so the engine's decision is kept apart from the user's choice; choosing the current category changes nothing. The category is assigned when the statement is imported, with the rules stored in the database, and is assigned again when the user applies the current rules: `rule` then holds the engine's current decision, in corrected transactions too (`null` when no rule matches); the initial set is seeded by the first migration. A rule has a text pattern, an amount sign (expense, income or any), a category and a priority. A rule matches a transaction when its sign fits the amount and its pattern appears in the description, ignoring case and accents. The pattern matches whole words; a `*` at its start or end also accepts a longer word that ends or starts with it (`*binter*` matches `bintercanarias`). When several rules match, the one with the lowest priority number wins. A priority has four digits: the first is its type (0 critical, 1 created by a user, 2 strong patterns, 3 generic patterns) and the other three are its number inside the type. The seeded rules have type 2 (2001 to 2018, one number per category). A user rule is numbered downwards from 1999, so the newest wins over the older ones and over every seeded rule. Rules with the same priority are ordered by id, both in the engine and in the list of rules.

In the development environment, the interactive documentation (Swagger UI) is available at `http://localhost:5236/swagger`.

## Requirements

- .NET 8 SDK (the version is pinned in `global.json`).
- SQL Server LocalDB, which is only available on Windows.

## Run

Run the commands from the repository root.

```bash
dotnet run --project src/backend/LexBank.Api --launch-profile http
```

The API listens on `http://localhost:5236`. On startup it applies the pending EF Core migrations, so the database is created automatically if it does not exist.

## Where the data is stored

In development the API uses **SQL Server LocalDB**, not a full SQL Server or SQL Express instance. The connection string is in `LexBank.Api/appsettings.Development.json`:

```
Server=(localdb)\MSSQLLocalDB;Database=LexBank;Integrated Security=true
```

LocalDB runs under your Windows user and starts on demand. It does not listen on the network, so it does not show up under `localhost\SQLEXPRESS` or any other instance: you must connect to it by its own name. The files are in your user folder (`LexBank.mdf` and `LexBank_log.ldf`); do not open them directly, SQL Server keeps them locked.

The `LexBank` database has four tables: `Transaction` (the imported transactions, each one linked to its user through `UserId` and, optionally, to the rule that categorized it through `RuleId`), `Rule` (the categorization rules, common to all users; the seeded rules have fixed ids: the number of the category's group times 100 plus the position in the category's list), `Users` (the accounts, with a unique index on `Email`) and `__EFMigrationsHistory` (the migrations already applied).

## Inspect the data

Connect to the server `(localdb)\MSSQLLocalDB` with Windows authentication.

- **SQL Server Management Studio:** new connection with that server name.
- **Rider:** Database tool window → `+` → Data Source → Microsoft SQL Server, host `(localdb)\MSSQLLocalDB`, database `LexBank`, Windows credentials. If the driver cannot resolve the name, use the instance pipe shown by `SqlLocalDB.exe info MSSQLLocalDB` (it changes every time the instance restarts).
- **Command line (Git Bash):**

  ```bash
  sqlcmd -S "(localdb)\\MSSQLLocalDB" -d LexBank -E -Q "SELECT TOP 5 * FROM [Transaction]"
  ```

  In Git Bash the backslash must be doubled. In PowerShell use a single one.

## Reset the database

Stop the API, drop the database and start the API again; the migrations recreate it with the seeded rules and no transactions.

```bash
./reset-db.sh
```

## Migrations

The EF Core CLI is a local tool (`.config/dotnet-tools.json`). Restore it once with `dotnet tool restore`. The development database holds no data worth keeping (the statements are re-imported from the source files), so while it stays that way the model changes are folded into a single `InitialCreate` migration instead of adding one per change: delete the contents of `Migrations` (`./reset-migrations.sh`), regenerate it and drop the database. The seeded categorization rules live in that migration, so a change to `InitialRules` requires the same steps.

```bash
dotnet ef migrations add InitialCreate --project src/backend/LexBank.Infrastructure --startup-project src/backend/LexBank.Api
```

Once data has to be preserved, each change is a new migration:

```bash
dotnet ef migrations add <Name> --project src/backend/LexBank.Infrastructure --startup-project src/backend/LexBank.Api
```

## Tests

```bash
dotnet test
```

The tests are written with xUnit and live under `tests/backend`, one project per tested layer:

| Project | What it tests |
|---|---|
| `LexBank.Infrastructure.Tests` | The Ruralvía XLSX and Revolut CSV readers, against fixture files, and the EF Core repositories, against a real database. |
| `LexBank.Api.Tests` | The HTTP endpoints end to end, with the API hosted in memory and a real database. |
| `LexBank.Domain.Tests` | Domain logic. |

The database tests use LocalDB too (see [ADR-0006](../../docs/adr/0006-localdb-for-database-tests.md)). Each test creates its own database and drops it at the end, so they never touch the `LexBank` development database.

## Continuous integration

The `backend` workflow (`.github/workflows/backend.yml`) builds the solution and runs the tests on every pull request into `main` that changes backend files. It runs on a Windows runner because the database tests need LocalDB. See [ADR-0003](../../docs/adr/0003-branching-ci-release.md).
