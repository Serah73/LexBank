using ClosedXML.Excel;
using LexBank.Application;
using LexBank.Domain;
using LexBank.Infrastructure;

namespace LexBank.Infrastructure.Tests;

public class RuralviaXlsxReaderTests
{
    private static FileStream OpenFixture() =>
        File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", "ruralvia.xlsx"));

    [Fact]
    public void Read_RuralviaStatement_ReturnsValidRowsWithDateDescriptionAmountEurAndBalance()
    {
        using var stream = OpenFixture();

        var result = new RuralviaXlsxReader().Read(stream);

        Assert.Equal(
            new[]
            {
                new Transaction(new DateOnly(2025, 1, 10), "COMPRA SUPERMERCADO EJEMPLO", -23.45m, "EUR", 1000.00m),
                new Transaction(new DateOnly(2025, 1, 11), "NOMINA EMPRESA EJEMPLO", 1500.00m, "EUR", 2476.55m),
                new Transaction(new DateOnly(2025, 1, 12), "RECIBO LUZ EJEMPLO", -60.10m, "EUR", 2416.45m),
            },
            result.Transactions);
    }

    [Fact]
    public void Read_RowWithInvalidDate_ReturnsFailedRowWithRowNumberAndReason()
    {
        using var stream = OpenFixture();

        var result = new RuralviaXlsxReader().Read(stream);

        Assert.Contains(new FailedRow(8, "Invalid date"), result.FailedRows);
    }

    [Fact]
    public void Read_RowWithInvalidAmount_ReturnsFailedRowWithRowNumberAndReason()
    {
        using var stream = OpenFixture();

        var result = new RuralviaXlsxReader().Read(stream);

        Assert.Contains(new FailedRow(9, "Invalid amount"), result.FailedRows);
    }

    [Fact]
    public void Read_RowWithInvalidBalance_ReturnsFailedRowWithRowNumberAndReason()
    {
        using var stream = OpenFixture();

        var result = new RuralviaXlsxReader().Read(stream);

        Assert.Contains(new FailedRow(10, "Invalid balance"), result.FailedRows);
    }

    [Fact]
    public void Read_XlsxWithoutRuralviaHeader_FailsWithNotRuralviaReason()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("other");
            sheet.Cell(4, 1).Value = "Date";
            sheet.Cell(4, 2).Value = "Amount";
            workbook.SaveAs(stream);
        }
        stream.Position = 0;

        var exception = Assert.Throws<UnsupportedStatementException>(() => new RuralviaXlsxReader().Read(stream));

        Assert.Equal("The file is not a Ruralvía XLSX statement", exception.Message);
    }
}
