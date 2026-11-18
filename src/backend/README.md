# LexBank backend

REST API of LexBank, built with ASP.NET Core 8, EF Core and SQL Server. It imports bank statements, normalizes them into the common transaction schema and persists them.

For the purpose and scope of the project, see the [main README](../../README.md).

## Architecture

The backend follows Clean Architecture ([ADR-0002](../../docs/adr/0002-clean-architecture.md)), with one project per layer:

| Project | Responsibility | References |
|---|---|---|
| `LexBank.Domain` | Business concepts: `Transaction`, `FailedRow`, `FailedRowReason`, `User`. | Nothing |
| `LexBank.Application` | Use cases (`UploadStatement`, `SignUp`, `LogIn`) and the interfaces they need (`IStatementReader`, `ITransactionRepository`, `IUserRepository`, `IPasswordHasher`). | Domain |
| `LexBank.Infrastructure` | Implementations: `RuralviaXlsxReader` (ClosedXML), `RevolutCsvReader`, `EfTransactionRepository`, `EfUserRepository` and `LexBankDbContext` (EF Core), `IdentityPasswordHasher` (password hashing with `Microsoft.Extensions.Identity.Core`), migrations. | Application |
| `LexBank.Api` | Minimal API endpoints and dependency injection. | Application, Infrastructure |

## API

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/statements` | Requires a session. Imports a statement sent as `multipart/form-data` in the `file` field. Returns the transactions read from the file, the rows that could not be read and `skippedTransactions` (the number of transactions not stored because they were already imported). Stores only the new valid transactions, for the user of the session. |
| `GET` | `/api/transactions` | Requires a session. Returns the stored transactions of the user of the session, newest first. |
| `POST` | `/api/users` | Creates a user account from a JSON body `{ "email", "password" }`. Returns `201 Created`. It does not start a session. A missing field is treated as empty. |
| `POST` | `/api/session` | Logs in with a JSON body `{ "email", "password" }`. Returns `200 OK` and sets the session cookie. A missing field is treated as empty. |
| `DELETE` | `/api/session` | Ends the session and removes the cookie. Returns `204 No Content`. |

The API returns stable codes instead of user-facing text; the frontend translates them. Enumerations are serialized as strings.

- A row that cannot be read is reported with its row number and a reason: `InvalidDate`, `InvalidAmount` or `InvalidBalance`.
- A file that is not a supported statement is rejected with `400 Bad Request` and the error code `UnsupportedStatement`. The reader is chosen by the file extension: `.xlsx` is read as a Ruralvía statement and `.csv` as a Revolut statement. The Revolut reader keeps only the completed transactions of the current account (`Actual`) and stores each amount minus its fee; a row with fewer fields than the header makes the whole file unsupported.
- A sign-up is rejected with `400 Bad Request` and the error code `InvalidEmail` or `InvalidPassword`, or with `409 Conflict` and `EmailAlreadyRegistered`. If both fields are invalid, only `InvalidEmail` is returned.
- A log-in with an unknown email or a wrong password is rejected with `401 Unauthorized` and the error code `InvalidCredentials`; the response is the same in both cases, so it does not reveal which emails are registered.
- A request to a protected endpoint without a valid session is answered with `401 Unauthorized` and no body. A session whose user no longer exists is rejected the same way.

Authentication uses a session cookie (`HttpOnly`, `SameSite=Strict`) that carries the email of the user; see [ADR-0007](../../docs/adr/0007-cookie-authentication.md).

An email is valid when it has at least one character before and after the first `@` and at most 256 characters. A password must have between 8 and 128 characters; there are no composition rules. Emails are compared without distinguishing case (the collation of SQL Server) and are stored as written. Passwords are stored only as a hash.

A transaction is considered already imported when a transaction stored for the same user has the same date, description, amount and balance. Identical rows inside the same file are kept: only as many copies as are already stored are skipped.

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

The `LexBank` database has three tables: `Transaction` (the imported transactions, each one linked to its user through `UserId`), `Users` (the accounts, with a unique index on `Email`) and `__EFMigrationsHistory` (the migrations already applied).

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

Stop the API, drop the database and start the API again; the migrations recreate it empty.

```bash
sqlcmd -S "(localdb)\\MSSQLLocalDB" -E -Q "DROP DATABASE LexBank"
```

## Migrations

The EF Core CLI is a local tool (`.config/dotnet-tools.json`). Restore it once with `dotnet tool restore`. To add a migration after changing the model:

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
