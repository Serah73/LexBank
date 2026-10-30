namespace LexBank.Domain;

public record FailedRow(int RowNumber, FailedRowReason Reason);
