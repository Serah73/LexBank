using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LexBank.Api.Tests;

public class StatementsEndpointTests : IDisposable
{
    private readonly LexBankApiFactory _factory = new();
    private readonly HttpClient _client;

    public StatementsEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static MultipartFormDataContent FixtureUpload()
    {
        var file = new StreamContent(File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", "ruralvia.xlsx")));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        return new MultipartFormDataContent { { file, "file", "ruralvia.xlsx" } };
    }

    [Fact]
    public async Task Upload_RuralviaStatement_ReturnsTransactionsWithDateDescriptionAmountAndBalance()
    {
        using var content = FixtureUpload();

        var response = await _client.PostAsync("/api/statements", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var transactions = body.RootElement.GetProperty("transactions");
        Assert.Equal(3, transactions.GetArrayLength());
        var first = transactions[0];
        Assert.Equal("2025-01-10", first.GetProperty("date").GetString());
        Assert.Equal("COMPRA SUPERMERCADO EJEMPLO", first.GetProperty("description").GetString());
        Assert.Equal(-23.45m, first.GetProperty("amount").GetDecimal());
        Assert.False(first.TryGetProperty("currency", out _));
        Assert.Equal(1000.00m, first.GetProperty("balance").GetDecimal());
    }

    [Fact]
    public async Task Upload_RuralviaStatement_SavesItsTransactionsSoTheyAreListedAfterwards()
    {
        using var content = FixtureUpload();
        await _client.PostAsync("/api/statements", content);

        var response = await _client.GetAsync("/api/transactions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var stored = body.RootElement.EnumerateArray()
            .Select(t => (t.GetProperty("date").GetString(), t.GetProperty("description").GetString(),
                t.GetProperty("amount").GetDecimal(), t.GetProperty("balance").GetDecimal()))
            .ToArray();
        Assert.Equal(
            new (string?, string?, decimal, decimal)[]
            {
                ("2025-01-12", "RECIBO LUZ EJEMPLO", -60.10m, 2416.45m),
                ("2025-01-11", "NOMINA EMPRESA EJEMPLO", 1500.00m, 2476.55m),
                ("2025-01-10", "COMPRA SUPERMERCADO EJEMPLO", -23.45m, 1000.00m),
            },
            stored);
    }

    [Fact]
    public async Task ListTransactions_WithNothingUploaded_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/transactions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, body.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Upload_RuralviaStatementWithUnreadableRows_SavesOnlyTheValidTransactionsAndStillReturnsTheFailedRows()
    {
        using var content = FixtureUpload();

        var uploadResponse = await _client.PostAsync("/api/statements", content);
        var listResponse = await _client.GetAsync("/api/transactions");

        using var uploaded = JsonDocument.Parse(await uploadResponse.Content.ReadAsStringAsync());
        Assert.Equal(3, uploaded.RootElement.GetProperty("failedRows").GetArrayLength());
        using var stored = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        Assert.Equal(3, stored.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Upload_FileThatIsNotRuralviaXlsx_SavesNothing()
    {
        using var content = new MultipartFormDataContent
        {
            { new ByteArrayContent("this is plain text, not a spreadsheet"u8.ToArray()), "file", "notes.txt" },
        };

        await _client.PostAsync("/api/statements", content);
        var response = await _client.GetAsync("/api/transactions");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, body.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Upload_RuralviaStatementWithUnreadableRows_ReturnsValidTransactionsAndFailedRows()
    {
        using var content = FixtureUpload();

        var response = await _client.PostAsync("/api/statements", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(3, body.RootElement.GetProperty("transactions").GetArrayLength());
        var failedRows = body.RootElement.GetProperty("failedRows")
            .EnumerateArray()
            .Select(r => (r.GetProperty("rowNumber").GetInt32(), r.GetProperty("reason").GetString()))
            .ToArray();
        Assert.Equal(
            new (int, string?)[] { (8, "InvalidDate"), (9, "InvalidAmount"), (10, "InvalidBalance") },
            failedRows);
    }

    [Fact]
    public async Task Upload_FileThatIsNotRuralviaXlsx_ReturnsBadRequestWithReasonAndNoTransactions()
    {
        using var content = new MultipartFormDataContent
        {
            { new ByteArrayContent("this is plain text, not a spreadsheet"u8.ToArray()), "file", "notes.txt" },
        };

        var response = await _client.PostAsync("/api/statements", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("NotRuralviaStatement", body.RootElement.GetProperty("error").GetString());
        Assert.False(body.RootElement.TryGetProperty("transactions", out _));
    }
}
