using LexBank.Domain;

namespace LexBank.Application;

public record UploadStatementResult(
    IReadOnlyList<Transaction> Transactions,
    IReadOnlyList<FailedRow> FailedRows,
    int SkippedTransactions);
