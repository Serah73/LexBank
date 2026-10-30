using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LexBank.Api.Tests;

public class StatementsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public StatementsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private static MultipartFormDataContent FixtureUpload()
    {
        var file = new StreamContent(File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", "ruralvia.xlsx")));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        return new MultipartFormDataContent { { file, "file", "ruralvia.xlsx" } };
    }

    [Fact]
    public async Task Upload_RuralviaStatement_ReturnsTransactionsWithDateDescriptionAmountCurrencyAndBalance()
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
        Assert.Equal("EUR", first.GetProperty("currency").GetString());
        Assert.Equal(1000.00m, first.GetProperty("balance").GetDecimal());
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
            new[] { (8, "Invalid date"), (9, "Invalid amount"), (10, "Invalid balance") },
            failedRows);
    }
}
