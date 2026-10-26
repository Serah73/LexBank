using ClosedXML.Excel;
using LexBank.Application;
using LexBank.Domain;

namespace LexBank.Infrastructure;

public class RuralviaXlsxReader
{
    private const int FirstDataRow = 5;

    public StatementReadResult Read(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);
        var transactions = new List<Transaction>();

        var failedRows = new List<FailedRow>();

        foreach (var row in sheet.RowsUsed().Where(r => r.RowNumber() >= FirstDataRow))
        {
            if (!row.Cell(1).TryGetValue<DateTime>(out var date))
            {
                failedRows.Add(new FailedRow(row.RowNumber(), "Invalid date"));
            }
            else if (row.Cell(4).TryGetValue<decimal>(out var amount)
                && row.Cell(5).TryGetValue<decimal>(out var balance))
            {
                transactions.Add(new Transaction(
                    DateOnly.FromDateTime(date), row.Cell(3).GetString(), amount, "EUR", balance));
            }
        }

        return new StatementReadResult(transactions, failedRows);
    }
}
