# LexBank backend

REST API of LexBank, built with ASP.NET Core 8, EF Core and SQL Server. It imports bank statements, normalizes them into the common transaction schema and persists them.

For the purpose and scope of the project, see the [main README](../../README.md).

## Architecture

The backend follows Clean Architecture ([ADR-0002](../../docs/adr/0002-clean-architecture.md)), with one project per layer:

| Project | Responsibility | References |
|---|---|---|
| `LexBank.Domain` | Business concepts: `Transaction`, `FailedRow`, `FailedRowReason`. | Nothing |
| `LexBank.Application` | Use cases (`UploadStatement`) and the interfaces they need (`IStatementReader`, `ITransactionRepository`). | Domain |
| `LexBank.Infrastructure` | Implementations: `RuralviaXlsxReader` (ClosedXML), `EfTransactionRepository` and `LexBankDbContext` (EF Core), migrations. | Application |
| `LexBank.Api` | Minimal API endpoints and dependency injection. | Application, Infrastructure |

## API

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/statements` | Imports a statement sent as `multipart/form-data` in the `file` field. Returns the transactions read from the file, the rows that could not be read and `skippedTransactions` (the number of transactions not stored because they were already imported). Stores only the new valid transactions. |
| `GET` | `/api/transactions` | Returns the stored transactions, newest first. |

The API returns stable codes instead of user-facing text; the frontend translates them. Enumerations are serialized as strings.

- A row that cannot be read is reported with its row number and a reason: `InvalidDate`, `InvalidAmount` or `InvalidBalance`.
- A file that is not a supported statement is rejected with `400 Bad Request` and the error code `NotRuralviaStatement`.

A transaction is considered already imported when a stored transaction has the same date, description, amount and balance. Identical rows inside the same file are kept: only as many copies as are already stored are skipped.

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

The `LexBank` database has two tables: `Transaction` (the imported transactions) and `__EFMigrationsHistory` (the migrations already applied).

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
| `LexBank.Infrastructure.Tests` | The Ruralvía XLSX reader, against a fixture file, and the EF Core repository, against a real database. |
| `LexBank.Api.Tests` | The HTTP endpoints end to end, with the API hosted in memory and a real database. |
| `LexBank.Domain.Tests` | Reserved for domain logic; it has no tests yet. |

The database tests use LocalDB too (see [ADR-0006](../../docs/adr/0006-localdb-for-database-tests.md)). Each test creates its own database and drops it at the end, so they never touch the `LexBank` development database.

## Continuous integration

The `backend` workflow (`.github/workflows/backend.yml`) builds the solution and runs the tests on every pull request that changes backend files. It runs on a Windows runner because the database tests need LocalDB. See [ADR-0003](../../docs/adr/0003-branching-ci-release.md).
