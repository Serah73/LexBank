using System.Globalization;
using System.Text;
using LexBank.Application;
using LexBank.Domain;

namespace LexBank.Infrastructure;

public class RevolutCsvReader : IStatementReader
{
    private const string NotRevolutMessage = "The file is not a Revolut CSV statement";

    private static readonly string[] RequiredHeaders =
        ["Producto", "Fecha de inicio", "Descripcion", "Importe", "Comision", "State", "Saldo"];

    public bool CanRead(string fileName) =>
        string.Equals(Path.GetExtension(fileName), ".csv", StringComparison.OrdinalIgnoreCase);

    public StatementReadResult Read(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var columns = FindColumns(Normalize(reader.ReadLine()), out var fieldCount);

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
            if (fields.Count < fieldCount)
            {
                throw new UnsupportedStatementException(
                    $"Line {lineNumber} of the Revolut CSV has {fields.Count} fields but the header has {fieldCount}");
            }

            if (fields[columns["Producto"]] != "Actual" || fields[columns["State"]] != "COMPLETADO")
            {
                continue;
            }

            if (!DateTime.TryParseExact(fields[columns["Fecha de inicio"]], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                failedRows.Add(new FailedRow(lineNumber, FailedRowReason.InvalidDate));
                continue;
            }

            if (!TryParseDecimal(fields[columns["Importe"]], out var grossAmount)
                || !TryParseDecimal(fields[columns["Comision"]], out var fee))
            {
                failedRows.Add(new FailedRow(lineNumber, FailedRowReason.InvalidAmount));
                continue;
            }

            if (!TryParseDecimal(fields[columns["Saldo"]], out var balance))
            {
                failedRows.Add(new FailedRow(lineNumber, FailedRowReason.InvalidBalance));
                continue;
            }

            transactions.Add(new Transaction(
                DateOnly.FromDateTime(date), fields[columns["Descripcion"]], grossAmount - fee, balance));
        }

        return new StatementReadResult(transactions, failedRows);
    }

    private static Dictionary<string, int> FindColumns(string? header, out int fieldCount)
    {
        if (header is null)
        {
            throw new UnsupportedStatementException(NotRevolutMessage);
        }

        var names = SplitFields(header);
        if (!RequiredHeaders.All(names.Contains))
        {
            throw new UnsupportedStatementException(NotRevolutMessage);
        }

        fieldCount = names.Count;
        return RequiredHeaders.ToDictionary(name => name, name => names.IndexOf(name));
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
}
