using System.Globalization;
using System.Text;
using LexBank.Application;
using LexBank.Domain;

namespace LexBank.Infrastructure;

public class RevolutCsvReader : IStatementReader
{
    private const string NotRevolutMessage = "The file is not a Revolut CSV statement";

    public bool CanRead(string fileName) =>
        string.Equals(Path.GetExtension(fileName), ".csv", StringComparison.OrdinalIgnoreCase);

    public StatementReadResult Read(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var columns = FindColumns(Normalize(reader.ReadLine()));

        var transactions = new List<Transaction>();
        var failedRows = new List<FailedRow>();
        var lineNumber = 1;
        while (Normalize(reader.ReadLine()) is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = SplitFields(line);
            if (fields.Count < columns.FieldCount)
            {
                throw new UnsupportedStatementException(
                    $"Line {lineNumber} of the Revolut CSV has {fields.Count} fields but the header has {columns.FieldCount}");
            }

            if (fields[columns.Product] != "Actual" || fields[columns.State] != "COMPLETADO")
            {
                continue;
            }

            var failure = TryParseRow(fields, columns, lineNumber, out var transaction);
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

    private static FailedRow? TryParseRow(List<string> fields, Columns columns, int lineNumber, out Transaction? transaction)
    {
        transaction = null;

        if (!DateTime.TryParseExact(fields[columns.Start], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return new FailedRow(lineNumber, FailedRowReason.InvalidDate);
        }

        if (!TryParseDecimal(fields[columns.Amount], out var amount) || !TryParseDecimal(fields[columns.Fee], out var fee))
        {
            return new FailedRow(lineNumber, FailedRowReason.InvalidAmount);
        }

        if (!TryParseDecimal(fields[columns.Balance], out var balance))
        {
            return new FailedRow(lineNumber, FailedRowReason.InvalidBalance);
        }

        transaction = new Transaction(DateOnly.FromDateTime(date), fields[columns.Description], amount - fee, balance, Bank.Revolut);
        return null;
    }

    private static Columns FindColumns(string? header)
    {
        if (header is null)
        {
            throw new UnsupportedStatementException(NotRevolutMessage);
        }

        var names = SplitFields(header);

        int Find(string name) =>
            names.IndexOf(name) is var index and >= 0 ? index : throw new UnsupportedStatementException(NotRevolutMessage);

        return new Columns(
            Find("Producto"), Find("Fecha de inicio"), Find("Descripcion"), Find("Importe"),
            Find("Comision"), Find("State"), Find("Saldo"), names.Count);
    }

    private static string? Normalize(string? line) =>
        line is null
            ? null
            : new string(line.Replace((char)0xA0, ' ').Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());

    private static bool TryParseDecimal(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);

    private static List<string> SplitFields(string line)
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        foreach (var character in line)
        {
            if (character == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (character == ',' && !inQuotes)
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else
            {
                field.Append(character);
            }
        }

        fields.Add(field.ToString());
        return fields;
    }

    private sealed record Columns(
        int Product, int Start, int Description, int Amount, int Fee, int State, int Balance, int FieldCount);
}
