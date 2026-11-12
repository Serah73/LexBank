# ADR-0004: Use ClosedXML to read XLSX statements

- Status: Accepted
- Date: 2026-10-25

## Context

- The first supported import format is XLSX, starting with statements exported by Ruralvía.
- .NET has no built-in API to read XLSX files, so a third-party library is needed.
- The library only needs to read workbooks; LexBank does not generate Excel files.
- The repository is licensed under CC BY-NC-SA 4.0, so the library's license must allow use in a non-commercial academic project without fees.
- Following the dependency rule (ADR-0002), the library is used only in `LexBank.Infrastructure`.

## Decision

Use **ClosedXML** (MIT) to read XLSX statements in `LexBank.Infrastructure`.

## Alternatives considered

1. **ExcelDataReader (MIT).** Read-only and also supports legacy `.xls`, but it exposes rows as untyped values with a lower-level API. Not chosen because `.xls` is out of scope and ClosedXML gives typed cell access with less code.
2. **Open XML SDK (`DocumentFormat.OpenXml`, MIT).** Official Microsoft library, but very verbose: shared strings, styles and number formats must be resolved by hand. ClosedXML is built on top of it.
3. **EPPlus.** Rejected because versions 5 and later use a commercial license (Polyform Noncommercial for non-commercial use), which adds licensing terms to track for no functional gain.

## Consequences

- **Positive:** simple, typed access to cells and rows; permissive license; actively maintained.
- **Negative:** ClosedXML is a read/write library, larger than needed for reading only, and brings transitive dependencies (Open XML SDK, SixLabors.Fonts, among others).
- The library is hidden behind an interface defined in the Application layer, so it can be replaced without affecting the domain or use cases.
- ClosedXML is acknowledged in `THIRD_PARTY_NOTICES.md`.
