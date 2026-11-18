using ClosedXML.Excel;
using LexBank.Application;
using LexBank.Domain;

namespace LexBank.Infrastructure;

public class RuralviaXlsxReader : IStatementReader
{
    private const int MaxHeaderRow = 10;
    private const string NotRuralviaMessage = "The file is not a Ruralvía XLSX statement";

    private static readonly string[] ExpectedHeaders =
        ["Fecha de la operación", "Fecha valor", "Tipo movimiento", "Importe", "Saldo"];

    public bool CanRead(string fileName) =>
        string.Equals(Path.GetExtension(fileName), ".xlsx", StringComparison.OrdinalIgnoreCase);

    public StatementReadResult Read(Stream stream)
    {
        using var workbook = Open(stream);
        var sheet = workbook.Worksheet(1);
        var (headerRow, columns) = FindHeader(sheet);

        var transactions = new List<Transaction>();
        var failedRows = new List<FailedRow>();

        foreach (var row in sheet.RowsUsed().Where(r => r.RowNumber() > headerRow))
        {
            var failure = TryParseRow(row, columns, out var transaction);
            if (failure is null)
            {
                transactions.Add(transaction!);
            }
            else
            {
                failedRows.Add(failure);
            }
        }

        return new StatementReadResult(transactions, failedRows);
    }

    private static (int Row, Columns Columns) FindHeader(IXLWorksheet sheet)
    {
        foreach (var row in Enumerable.Range(1, MaxHeaderRow))
        {
            var names = sheet.Row(row).CellsUsed().ToDictionary(c => c.GetString(), c => c.Address.ColumnNumber);
            if (ExpectedHeaders.All(names.ContainsKey))
            {
                return (row, new Columns(
                    names["Fecha de la operación"], names["Tipo movimiento"], names["Importe"], names["Saldo"]));
            }
        }

        throw new UnsupportedStatementException(NotRuralviaMessage);
    }

    private static XLWorkbook Open(Stream stream)
    {
        try
        {
            return new XLWorkbook(stream);
        }
        catch (Exception exception)
        {
            throw new UnsupportedStatementException(NotRuralviaMessage, exception);
        }
    }

    private static FailedRow? TryParseRow(IXLRow row, Columns columns, out Transaction? transaction)
    {
        transaction = null;

        if (!row.Cell(columns.Date).TryGetValue<DateTime>(out var date))
        {
            return new FailedRow(row.RowNumber(), FailedRowReason.InvalidDate);
        }

        if (!row.Cell(columns.Amount).TryGetValue<decimal>(out var amount))
        {
            return new FailedRow(row.RowNumber(), FailedRowReason.InvalidAmount);
        }

        if (!row.Cell(columns.Balance).TryGetValue<decimal>(out var balance))
        {
            return new FailedRow(row.RowNumber(), FailedRowReason.InvalidBalance);
        }

        transaction = new Transaction(DateOnly.FromDateTime(date), row.Cell(columns.Description).GetString(), amount, balance);
        return null;
    }

    private sealed record Columns(int Date, int Description, int Amount, int Balance);
}
