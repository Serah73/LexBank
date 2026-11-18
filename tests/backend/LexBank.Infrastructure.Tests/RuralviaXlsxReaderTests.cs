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
    public void Read_RuralviaStatement_ReturnsValidRowsWithDateDescriptionAmountAndBalance()
    {
        using var stream = OpenFixture();

        var result = new RuralviaXlsxReader().Read(stream);

        Assert.Equal(
            new[]
            {
                new Transaction(new DateOnly(2025, 1, 10), "COMPRA SUPERMERCADO EJEMPLO", -23.45m, 1000.00m),
                new Transaction(new DateOnly(2025, 1, 11), "NOMINA EMPRESA EJEMPLO", 1500.00m, 2476.55m),
                new Transaction(new DateOnly(2025, 1, 12), "RECIBO LUZ EJEMPLO", -60.10m, 2416.45m),
            },
            result.Transactions);
    }

    [Theory]
    [InlineData("statement.xlsx", true)]
    [InlineData("STATEMENT.XLSX", true)]
    [InlineData("statement.csv", false)]
    [InlineData("statement", false)]
    public void CanRead_FileName_IsTrueOnlyForXlsxExtension(string fileName, bool expected)
    {
        Assert.Equal(expected, new RuralviaXlsxReader().CanRead(fileName));
    }

    [Fact]
    public void Read_RowWithInvalidDate_ReturnsFailedRowWithRowNumberAndReason()
    {
        using var stream = OpenFixture();

        var result = new RuralviaXlsxReader().Read(stream);

        Assert.Contains(new FailedRow(8, FailedRowReason.InvalidDate), result.FailedRows);
    }

    [Fact]
    public void Read_RowWithInvalidAmount_ReturnsFailedRowWithRowNumberAndReason()
    {
        using var stream = OpenFixture();

        var result = new RuralviaXlsxReader().Read(stream);

        Assert.Contains(new FailedRow(9, FailedRowReason.InvalidAmount), result.FailedRows);
    }

    [Fact]
    public void Read_RowWithInvalidBalance_ReturnsFailedRowWithRowNumberAndReason()
    {
        using var stream = OpenFixture();

        var result = new RuralviaXlsxReader().Read(stream);

        Assert.Contains(new FailedRow(10, FailedRowReason.InvalidBalance), result.FailedRows);
    }

    [Fact]
    public void Read_HeaderInAnotherRow_ReadsTheRowsBelowIt()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("movements");
            string[] headers = ["Fecha de la operación", "Fecha valor", "Tipo movimiento", "Importe", "Saldo"];
            for (var column = 0; column < headers.Length; column++)
            {
                sheet.Cell(6, column + 1).Value = headers[column];
            }
            sheet.Cell(7, 1).Value = new DateTime(2025, 2, 3);
            sheet.Cell(7, 3).Value = "COMPRA EJEMPLO";
            sheet.Cell(7, 4).Value = -12.50m;
            sheet.Cell(7, 5).Value = 987.50m;
            workbook.SaveAs(stream);
        }
        stream.Position = 0;

        var result = new RuralviaXlsxReader().Read(stream);

        Assert.Equal(
            [new Transaction(new DateOnly(2025, 2, 3), "COMPRA EJEMPLO", -12.50m, 987.50m)],
            result.Transactions);
    }

    [Fact]
    public void Read_ColumnsInAnotherOrderWithAnExtraColumn_FindsThemByHeaderName()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("movements");
            string[] headers = ["Saldo", "Referencia", "Importe", "Tipo movimiento", "Fecha valor", "Fecha de la operación"];
            for (var column = 0; column < headers.Length; column++)
            {
                sheet.Cell(4, column + 1).Value = headers[column];
            }
            sheet.Cell(5, 1).Value = 987.50m;
            sheet.Cell(5, 2).Value = "REF-1";
            sheet.Cell(5, 3).Value = -12.50m;
            sheet.Cell(5, 4).Value = "COMPRA EJEMPLO";
            sheet.Cell(5, 6).Value = new DateTime(2025, 2, 3);
            workbook.SaveAs(stream);
        }
        stream.Position = 0;

        var result = new RuralviaXlsxReader().Read(stream);

        Assert.Equal(
            [new Transaction(new DateOnly(2025, 2, 3), "COMPRA EJEMPLO", -12.50m, 987.50m)],
            result.Transactions);
    }

    [Fact]
    public void Read_HeaderBeyondTheFirstTenRows_FailsWithNotRuralviaReason()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("movements");
            string[] headers = ["Fecha de la operación", "Fecha valor", "Tipo movimiento", "Importe", "Saldo"];
            for (var column = 0; column < headers.Length; column++)
            {
                sheet.Cell(11, column + 1).Value = headers[column];
            }
            workbook.SaveAs(stream);
        }
        stream.Position = 0;

        var exception = Assert.Throws<UnsupportedStatementException>(() => new RuralviaXlsxReader().Read(stream));

        Assert.Equal("The file is not a Ruralvía XLSX statement", exception.Message);
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

    [Fact]
    public void Read_FileThatIsNotXlsx_FailsWithNotRuralviaReason()
    {
        using var stream = new MemoryStream("this is plain text, not a spreadsheet"u8.ToArray());

        var exception = Assert.Throws<UnsupportedStatementException>(() => new RuralviaXlsxReader().Read(stream));

        Assert.Equal("The file is not a Ruralvía XLSX statement", exception.Message);
    }

    [Fact]
    public void Read_ZipThatIsNotXlsx_FailsWithNotRuralviaReason()
    {
        using var stream = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            zip.CreateEntry("notes.txt");
        }
        stream.Position = 0;

        var exception = Assert.Throws<UnsupportedStatementException>(() => new RuralviaXlsxReader().Read(stream));

        Assert.Equal("The file is not a Ruralvía XLSX statement", exception.Message);
    }
}
