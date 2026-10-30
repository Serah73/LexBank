using ClosedXML.Excel;
using LexBank.Application;
using LexBank.Domain;

namespace LexBank.Infrastructure;

public class RuralviaXlsxReader : IStatementReader
{
    private const int HeaderRow = 4;
    private const int FirstDataRow = 5;
    private const string NotRuralviaMessage = "The file is not a Ruralvía XLSX statement";

    private static readonly string[] ExpectedHeaders =
        ["Fecha de la operación", "Fecha valor", "Tipo movimiento", "Importe", "Saldo"];

    public StatementReadResult Read(Stream stream)
    {
        using var workbook = Open(stream);
        var sheet = workbook.Worksheet(1);
        var headers = Enumerable.Range(1, ExpectedHeaders.Length).Select(c => sheet.Cell(HeaderRow, c).GetString());
        if (!headers.SequenceEqual(ExpectedHeaders))
        {
            throw new UnsupportedStatementException(NotRuralviaMessage);
        }

        var transactions = new List<Transaction>();
        var failedRows = new List<FailedRow>();

        foreach (var row in sheet.RowsUsed().Where(r => r.RowNumber() >= FirstDataRow))
        {
            var failure = TryParseRow(row, out var transaction);
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

    private static XLWorkbook Open(Stream stream)
    {
        try
        {
            return new XLWorkbook(stream);
        }
        catch (FileFormatException)
        {
            throw new UnsupportedStatementException(NotRuralviaMessage);
        }
    }

    private static FailedRow? TryParseRow(IXLRow row, out Transaction? transaction)
    {
        transaction = null;

        if (!row.Cell(1).TryGetValue<DateTime>(out var date))
        {
            return new FailedRow(row.RowNumber(), "Invalid date");
        }

        if (!row.Cell(4).TryGetValue<decimal>(out var amount))
        {
            return new FailedRow(row.RowNumber(), "Invalid amount");
        }

        if (!row.Cell(5).TryGetValue<decimal>(out var balance))
        {
            return new FailedRow(row.RowNumber(), "Invalid balance");
        }

        transaction = new Transaction(DateOnly.FromDateTime(date), row.Cell(3).GetString(), amount, "EUR", balance);
        return null;
    }
}
