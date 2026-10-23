# LexBank

Web application to normalize heterogeneous Spanish bank statements and categorize transactions with a deterministic rules engine.

## About

LexBank is the Master's Thesis (TFM) for the Master's degree in Computer Engineering at the Universitat Oberta de Catalunya (UOC), in the web development area.

**Thesis title:** *Design and development of a web application for the normalization and integration of heterogeneous Spanish bank statements and their automatic categorization through a deterministic rules engine.*

**Author:** José Ramón Rodríguez Hernández ([@Serah73](https://github.com/Serah73))

## Objective

Each Spanish bank exports statements in its own format. LexBank imports these statements, normalizes them into a common, bank-independent schema, and assigns each transaction a category (e.g. groceries, leisure, health) using a keyword-based rules engine.

The system deliberately does **not** use AI/ML for categorization. Users can manually correct categories, and those corrections also provide data to measure the engine's accuracy.

Core use cases:

1. Import a bank statement (CSV, Excel, PDF), with automatic header mapping (manual mapping as an extension).
2. Detect and map headers (date, description, amount, currency, balance) to the common schema.
3. Categorize transactions with the rules engine.
4. Review and correct categories.

## Tech stack

- **Backend:** ASP.NET Core 8, EF Core, SQL Server, REST API.
- **Frontend (planned, not started):** React + TypeScript, shadcn/ui, Tailwind CSS, Recharts.
- **Practices:** Clean Architecture (Domain, Application, Infrastructure, Api), TDD, Architecture Decision Records (ADRs), monorepo.

## Repository structure

```
src/backend/     Backend source code
tests/backend/   Backend tests
docs/adr/        Architecture Decision Records
skills/          Curated AI-agent skills (Claude Code) used during development
```

`skills/` documents the AI-agent tooling used to support development of this project.

## Status

Early development — the project is at the scaffolding stage.

## License

This project is licensed under the [Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International](LICENSE) license (CC BY-NC-SA 4.0).

Third-party code licensed under MIT is acknowledged in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
