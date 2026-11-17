using System.Text;
using LexBank.Application;
using LexBank.Domain;
using LexBank.Infrastructure;

namespace LexBank.Infrastructure.Tests;

public class RevolutCsvReaderTests
{
    private const string Header = "Tipo,Producto,Fecha de inicio,Fecha de finalización,Descripción,Importe,Comisión,Divisa,State,Saldo";

    private static MemoryStream Csv(params string[] rows) =>
        new(Encoding.UTF8.GetBytes(string.Join("\n", new[] { Header }.Concat(rows))));

    private static string Row(
        string product = "Actual",
        string start = "2025-01-10 09:15:00",
        string description = "Supermercado Ejemplo",
        string amount = "-23.45",
        string fee = "0.00",
        string state = "COMPLETADO",
        string balance = "1000.00") =>
        $"Pago,{product},{start},{start},{description},{amount},{fee},EUR,{state},{balance}";

    [Fact]
    public void Read_RevolutStatement_ReturnsCompletedTransactionsWithDateDescriptionAmountAndBalance()
    {
        using var stream = Csv(
            Row(),
            Row(start: "2025-01-11 10:00:00", description: "Nomina Empresa Ejemplo", amount: "1500.00", balance: "2476.55"));

        var result = new RevolutCsvReader().Read(stream);

        Assert.Equal(
            new[]
            {
                new Transaction(new DateOnly(2025, 1, 10), "Supermercado Ejemplo", -23.45m, 1000.00m),
                new Transaction(new DateOnly(2025, 1, 11), "Nomina Empresa Ejemplo", 1500.00m, 2476.55m),
            },
            result.Transactions);
    }

    [Fact]
    public void Read_SavingsDepositRows_AreLeftOutAndNotReportedAsFailed()
    {
        using var stream = Csv(Row(product: "Depósito", description: "Interés ejemplo", amount: "0.50", balance: "500.50"));

        var result = new RevolutCsvReader().Read(stream);

        Assert.Empty(result.Transactions);
        Assert.Empty(result.FailedRows);
    }

    [Fact]
    public void Read_NotCompletedRows_AreLeftOutAndNotReportedAsFailed()
    {
        using var stream = Csv(Row(description: "Pago pendiente", amount: "-5.00", state: "PENDIENTE", balance: "995.00"));

        var result = new RevolutCsvReader().Read(stream);

        Assert.Empty(result.Transactions);
        Assert.Empty(result.FailedRows);
    }

    [Fact]
    public void Read_RowWithInvalidDate_ReturnsFailedRowWithLineNumberAndReason()
    {
        using var stream = Csv(Row(), Row(start: "not-a-date"));

        var result = new RevolutCsvReader().Read(stream);

        Assert.Equal(new[] { new FailedRow(3, FailedRowReason.InvalidDate) }, result.FailedRows);
        Assert.Single(result.Transactions);
    }

    [Fact]
    public void Read_RowWithInvalidAmount_ReturnsFailedRowWithLineNumberAndReason()
    {
        using var stream = Csv(Row(), Row(amount: "abc"));

        var result = new RevolutCsvReader().Read(stream);

        Assert.Equal(new[] { new FailedRow(3, FailedRowReason.InvalidAmount) }, result.FailedRows);
        Assert.Single(result.Transactions);
    }

    [Fact]
    public void Read_RowWithInvalidBalance_ReturnsFailedRowWithLineNumberAndReason()
    {
        using var stream = Csv(Row(), Row(balance: "xyz"));

        var result = new RevolutCsvReader().Read(stream);

        Assert.Equal(new[] { new FailedRow(3, FailedRowReason.InvalidBalance) }, result.FailedRows);
        Assert.Single(result.Transactions);
    }

    [Fact]
    public void Read_CsvWithoutRevolutHeaders_FailsAsNotRevolut()
    {
        using var stream = new MemoryStream("Date,Amount\n2025-01-10,5"u8.ToArray());

        var exception = Assert.Throws<UnsupportedStatementException>(() => new RevolutCsvReader().Read(stream));

        Assert.Equal("The file is not a Revolut CSV statement", exception.Message);
    }

    [Fact]
    public void Read_FileThatIsNotCsv_FailsAsNotRevolut()
    {
        using var stream = new MemoryStream("this is plain text, not a statement"u8.ToArray());

        var exception = Assert.Throws<UnsupportedStatementException>(() => new RevolutCsvReader().Read(stream));

        Assert.Equal("The file is not a Revolut CSV statement", exception.Message);
    }

    [Fact]
    public void Read_ColumnsInAnotherOrderWithAnExtraColumn_FindsThemByHeaderName()
    {
        const string content =
            "Saldo,Extra,State,Comisión,Importe,Descripción,Fecha de inicio,Producto\n" +
            "1000.00,x,COMPLETADO,1.00,-10.00,Cafe Ejemplo,2025-01-10 09:15:00,Actual";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var result = new RevolutCsvReader().Read(stream);

        Assert.Equal(
            new[] { new Transaction(new DateOnly(2025, 1, 10), "Cafe Ejemplo", -11.00m, 1000.00m) },
            result.Transactions);
    }

    [Theory]
    [InlineData("statement.csv", true)]
    [InlineData("STATEMENT.CSV", true)]
    [InlineData("statement.xlsx", false)]
    [InlineData("statement", false)]
    public void CanRead_FileName_IsTrueOnlyForCsvExtension(string fileName, bool expected)
    {
        Assert.Equal(expected, new RevolutCsvReader().CanRead(fileName));
    }

    [Fact]
    public void Read_DescriptionWithNonBreakingSpace_ReplacesItWithASpace()
    {
        using var stream = Csv(Row(description: "Cuenta remunerada"));

        var result = new RevolutCsvReader().Read(stream);

        Assert.Equal("Cuenta remunerada", result.Transactions.Single().Description);
    }

    [Fact]
    public void Read_DescriptionWithAccents_StoresItWithoutAccents()
    {
        using var stream = Csv(Row(description: "\"Café Ejemplo, Niño\""));

        var result = new RevolutCsvReader().Read(stream);

        Assert.Equal("Cafe Ejemplo, Nino", result.Transactions.Single().Description);
    }

    [Fact]
    public void Read_QuotedDescriptionWithDateComma_KeepsTheDescriptionAndReadsTheBalance()
    {
        using var stream = Csv(Row(description: "\"Intereses cobrados el 17 de noviembre, 2026\"", balance: "500.50"));

        var result = new RevolutCsvReader().Read(stream);

        Assert.Equal(
            new[] { new Transaction(new DateOnly(2025, 1, 10), "Intereses cobrados el 17 de noviembre, 2026", -23.45m, 500.50m) },
            result.Transactions);
    }

    [Fact]
    public void Read_RowWithFewerFieldsThanTheHeader_FailsExplainingWhichLineAndWhy()
    {
        using var stream = Csv(Row(), "Pago,Actual,2025-01-10 09:15:00");

        var exception = Assert.Throws<UnsupportedStatementException>(() => new RevolutCsvReader().Read(stream));

        Assert.Equal("Line 3 of the Revolut CSV has 3 fields but the header has 10", exception.Message);
    }

    [Fact]
    public void Read_BlankLines_AreSkippedWithoutFailing()
    {
        using var stream = Csv(Row(), "", Row(start: "2025-01-11 10:00:00"), "");

        var result = new RevolutCsvReader().Read(stream);

        Assert.Equal(2, result.Transactions.Count);
        Assert.Empty(result.FailedRows);
    }

    [Fact]
    public void Read_RowWithFee_StoresAmountMinusFee()
    {
        using var stream = Csv(Row(description: "Cajero Ejemplo", amount: "-50.00", fee: "1.50", balance: "2425.05"));

        var result = new RevolutCsvReader().Read(stream);

        Assert.Equal(
            new[] { new Transaction(new DateOnly(2025, 1, 10), "Cajero Ejemplo", -51.50m, 2425.05m) },
            result.Transactions);
    }

    [Fact]
    public void Read_DescriptionWithCommaInQuotes_KeepsTheWholeDescription()
    {
        using var stream = Csv(Row(description: "\"Nomina, Empresa Ejemplo\"", amount: "1500.00", balance: "2476.55"));

        var result = new RevolutCsvReader().Read(stream);

        Assert.Equal(
            new[] { new Transaction(new DateOnly(2025, 1, 10), "Nomina, Empresa Ejemplo", 1500.00m, 2476.55m) },
            result.Transactions);
    }
}
