using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using LexBank.Application;
using LexBank.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LexBank.Api.Tests;

public class StatementsEndpointTests : IAsyncLifetime
{
    private readonly LexBankApiFactory _factory = new();
    private readonly HttpClient _client;

    public StatementsEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync() => await _client.LogInAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static MultipartFormDataContent FixtureUpload()
    {
        var file = new StreamContent(File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", "ruralvia.xlsx")));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        return new MultipartFormDataContent { { file, "file", "ruralvia.xlsx" } };
    }

    private static MultipartFormDataContent RevolutFixtureUpload()
    {
        var file = new StreamContent(File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", "revolut.csv")));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        return new MultipartFormDataContent { { file, "file", "revolut.csv" } };
    }

    private static MultipartFormDataContent AnyFileUpload() =>
        new() { { new ByteArrayContent("any content"u8.ToArray()), "file", "statement.xlsx" } };

    private async Task<HttpClient> ClientReadingWithAsync(IStatementReader reader)
    {
        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IStatementReader>();
                services.AddSingleton(reader);
            })).CreateClient();
        await client.LogInAsync();
        return client;
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
    public async Task Upload_RuleMatches_ReturnsTheCategoryOfTheRule()
    {
        var reader = new StubStatementReader
        {
            Transactions = [new Transaction(new DateOnly(2025, 1, 10), "tj-mercadona finca espana", -23.40m, 100m, Bank.Ruralvia)],
        };
        using var client = await ClientReadingWithAsync(reader);

        var response = await client.PostAsync("/api/statements", AnyFileUpload());

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var first = body.RootElement.GetProperty("transactions")[0];
        Assert.Equal("Groceries", first.GetProperty("category").GetString());
    }

    [Fact]
    public async Task Upload_NoRuleMatches_ReturnsANullCategory()
    {
        var reader = new StubStatementReader
        {
            Transactions = [new Transaction(new DateOnly(2025, 1, 10), "tj-fauca", -4.80m, 100m, Bank.Ruralvia)],
        };
        using var client = await ClientReadingWithAsync(reader);

        var response = await client.PostAsync("/api/statements", AnyFileUpload());

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var first = body.RootElement.GetProperty("transactions")[0];
        Assert.Equal(JsonValueKind.Null, first.GetProperty("category").ValueKind);
    }

    [Fact]
    public async Task Upload_SameCategorizedStatementTwice_SkipsTheDuplicates()
    {
        var reader = new StubStatementReader
        {
            Transactions = [new Transaction(new DateOnly(2025, 1, 10), "tj-mercadona finca espana", -23.40m, 100m, Bank.Ruralvia)],
        };
        using var client = await ClientReadingWithAsync(reader);

        await client.PostAsync("/api/statements", AnyFileUpload());
        var second = await client.PostAsync("/api/statements", AnyFileUpload());
        var stored = await client.GetAsync("/api/transactions");

        using var secondBody = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        Assert.Equal(1, secondBody.RootElement.GetProperty("skippedTransactions").GetInt32());
        using var storedBody = JsonDocument.Parse(await stored.Content.ReadAsStringAsync());
        Assert.Equal(1, storedBody.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task ListTransactions_AfterAnUpload_ReturnsTheStoredCategory()
    {
        var reader = new StubStatementReader
        {
            Transactions =
            [
                new Transaction(new DateOnly(2025, 1, 10), "tj-mercadona finca espana", -23.40m, 100m, Bank.Ruralvia),
                new Transaction(new DateOnly(2025, 1, 9), "tj-fauca", -4.80m, 123.40m, Bank.Ruralvia),
            ],
        };
        using var client = await ClientReadingWithAsync(reader);
        await client.PostAsync("/api/statements", AnyFileUpload());

        var response = await client.GetAsync("/api/transactions");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var categories = body.RootElement.EnumerateArray().Select(t => t.GetProperty("category").GetString());
        Assert.Equal(["Groceries", null], categories);
    }

    [Fact]
    public async Task Upload_RuralviaStatement_ReturnsTransactionsWithTheRuralviaBankCode()
    {
        using var content = FixtureUpload();

        var response = await _client.PostAsync("/api/statements", content);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var banks = body.RootElement.GetProperty("transactions").EnumerateArray().Select(t => t.GetProperty("bank").GetString());
        Assert.All(banks, bank => Assert.Equal("Ruralvia", bank));
    }

    [Fact]
    public async Task ListTransactions_AfterImportingARevolutStatement_ReturnsTheRevolutBankCode()
    {
        using var content = RevolutFixtureUpload();
        await _client.PostAsync("/api/statements", content);

        var response = await _client.GetAsync("/api/transactions");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var banks = body.RootElement.EnumerateArray().Select(t => t.GetProperty("bank").GetString());
        Assert.NotEmpty(banks);
        Assert.All(banks, bank => Assert.Equal("Revolut", bank));
    }

    [Fact]
    public async Task Upload_RevolutCsv_ReturnsTheCompletedCurrentAccountTransactions()
    {
        using var content = RevolutFixtureUpload();

        var response = await _client.PostAsync("/api/statements", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var transactions = body.RootElement.GetProperty("transactions").EnumerateArray()
            .Select(t => (t.GetProperty("date").GetString(), t.GetProperty("description").GetString(),
                t.GetProperty("amount").GetDecimal(), t.GetProperty("balance").GetDecimal()))
            .ToArray();
        Assert.Equal(
            new (string?, string?, decimal, decimal)[]
            {
                ("2025-01-10", "Supermercado Ejemplo", -23.45m, 1000.00m),
                ("2025-01-11", "Nomina, Empresa Ejemplo", 1500.00m, 2476.55m),
                ("2025-01-12", "Cajero Ejemplo", -51.50m, 2425.05m),
            },
            transactions);
    }

    [Fact]
    public async Task Upload_RevolutCsvWithUnreadableRows_ReturnsThemAsFailedRows()
    {
        using var content = RevolutFixtureUpload();

        var response = await _client.PostAsync("/api/statements", content);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var failedRows = body.RootElement.GetProperty("failedRows")
            .EnumerateArray()
            .Select(r => (r.GetProperty("rowNumber").GetInt32(), r.GetProperty("reason").GetString()))
            .ToArray();
        Assert.Equal(
            new (int, string?)[] { (7, "InvalidDate"), (8, "InvalidAmount"), (9, "InvalidBalance") },
            failedRows);
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
    public async Task Upload_SameStatementTwice_StoresEachTransactionOnce()
    {
        using var first = FixtureUpload();
        using var second = FixtureUpload();
        await _client.PostAsync("/api/statements", first);
        await _client.PostAsync("/api/statements", second);

        var response = await _client.GetAsync("/api/transactions");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(3, body.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Upload_OverlappingStatement_StoresOnlyTheNewTransactions()
    {
        var reader = new StubStatementReader();
        using var client = await ClientReadingWithAsync(reader);
        var january = new Transaction(new DateOnly(2025, 1, 10), "COMPRA ENERO", -10m, 100m, Bank.Ruralvia);
        var february = new Transaction(new DateOnly(2025, 2, 10), "COMPRA FEBRERO", -20m, 80m, Bank.Ruralvia);
        var march = new Transaction(new DateOnly(2025, 3, 10), "COMPRA MARZO", -30m, 50m, Bank.Ruralvia);

        reader.Transactions = [january, february];
        await client.PostAsync("/api/statements", AnyFileUpload());
        reader.Transactions = [february, march];
        await client.PostAsync("/api/statements", AnyFileUpload());
        var response = await client.GetAsync("/api/transactions");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var descriptions = body.RootElement.EnumerateArray().Select(t => t.GetProperty("description").GetString());
        Assert.Equal(["COMPRA MARZO", "COMPRA FEBRERO", "COMPRA ENERO"], descriptions);
    }

    [Fact]
    public async Task Upload_RowsIdenticalInsideOneFile_StoresAllOfThem()
    {
        var reader = new StubStatementReader();
        using var client = await ClientReadingWithAsync(reader);
        reader.Transactions =
        [
            new Transaction(new DateOnly(2025, 1, 10), "CAFE", -1.50m, 100m, Bank.Ruralvia),
            new Transaction(new DateOnly(2025, 1, 10), "CAFE", -1.50m, 100m, Bank.Ruralvia),
        ];

        await client.PostAsync("/api/statements", AnyFileUpload());
        var response = await client.GetAsync("/api/transactions");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(2, body.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Upload_MoreCopiesOfARowThanAreStored_StoresTheMissingOnes()
    {
        var reader = new StubStatementReader();
        using var client = await ClientReadingWithAsync(reader);
        reader.Transactions = [new Transaction(new DateOnly(2025, 1, 10), "CAFE", -1.50m, 100m, Bank.Ruralvia)];
        await client.PostAsync("/api/statements", AnyFileUpload());
        reader.Transactions =
        [
            new Transaction(new DateOnly(2025, 1, 10), "CAFE", -1.50m, 100m, Bank.Ruralvia),
            new Transaction(new DateOnly(2025, 1, 10), "CAFE", -1.50m, 100m, Bank.Ruralvia),
        ];

        await client.PostAsync("/api/statements", AnyFileUpload());
        var response = await client.GetAsync("/api/transactions");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(2, body.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Upload_SameRowWithDifferentBalance_IsStoredAsNew()
    {
        var reader = new StubStatementReader();
        using var client = await ClientReadingWithAsync(reader);
        reader.Transactions = [new Transaction(new DateOnly(2025, 1, 10), "CAFE", -1.50m, 100m, Bank.Ruralvia)];
        await client.PostAsync("/api/statements", AnyFileUpload());
        reader.Transactions = [new Transaction(new DateOnly(2025, 1, 10), "CAFE", -1.50m, 90m, Bank.Ruralvia)];

        await client.PostAsync("/api/statements", AnyFileUpload());
        var response = await client.GetAsync("/api/transactions");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(2, body.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Upload_StatementAlreadyStored_StillReturnsItsTransactionsAndFailedRows()
    {
        using var first = FixtureUpload();
        using var second = FixtureUpload();
        await _client.PostAsync("/api/statements", first);

        var response = await _client.PostAsync("/api/statements", second);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(3, body.RootElement.GetProperty("transactions").GetArrayLength());
        Assert.Equal(3, body.RootElement.GetProperty("failedRows").GetArrayLength());
    }

    [Fact]
    public async Task Upload_OverlappingStatement_ReportsHowManyWereSkipped()
    {
        var reader = new StubStatementReader();
        using var client = await ClientReadingWithAsync(reader);
        var january = new Transaction(new DateOnly(2025, 1, 10), "COMPRA ENERO", -10m, 100m, Bank.Ruralvia);
        var february = new Transaction(new DateOnly(2025, 2, 10), "COMPRA FEBRERO", -20m, 80m, Bank.Ruralvia);
        var march = new Transaction(new DateOnly(2025, 3, 10), "COMPRA MARZO", -30m, 50m, Bank.Ruralvia);
        reader.Transactions = [january, february];
        await client.PostAsync("/api/statements", AnyFileUpload());
        reader.Transactions = [february, march];

        var response = await client.PostAsync("/api/statements", AnyFileUpload());

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, body.RootElement.GetProperty("skippedTransactions").GetInt32());
    }

    [Fact]
    public async Task Upload_NewStatement_ReportsZeroSkipped()
    {
        using var content = FixtureUpload();

        var response = await _client.PostAsync("/api/statements", content);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, body.RootElement.GetProperty("skippedTransactions").GetInt32());
    }

    [Fact]
    public async Task GetTransactions_AnotherUserImportedAStatement_ReturnsOnlyOwnTransactions()
    {
        using var other = _factory.CreateClient();
        await other.LogInAsync("luis@example.com");
        using var content = FixtureUpload();
        await _client.PostAsync("/api/statements", content);

        var response = await other.GetAsync("/api/transactions");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, body.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task PostStatements_AnotherUserImportedTheSameStatement_StoresEveryTransaction()
    {
        using var other = _factory.CreateClient();
        await other.LogInAsync("luis@example.com");
        using var first = FixtureUpload();
        using var second = FixtureUpload();
        await _client.PostAsync("/api/statements", first);

        var response = await other.PostAsync("/api/statements", second);
        var listResponse = await other.GetAsync("/api/transactions");

        using var uploaded = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, uploaded.RootElement.GetProperty("skippedTransactions").GetInt32());
        using var stored = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        Assert.Equal(3, stored.RootElement.GetArrayLength());
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
    public async Task Upload_UnsupportedFile_SavesNothing()
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
    public async Task Upload_UnsupportedFile_ReturnsBadRequestWithUnsupportedStatementCodeAndNoTransactions()
    {
        using var content = new MultipartFormDataContent
        {
            { new ByteArrayContent("this is plain text, not a spreadsheet"u8.ToArray()), "file", "notes.txt" },
        };

        var response = await _client.PostAsync("/api/statements", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("UnsupportedStatement", body.RootElement.GetProperty("error").GetString());
        Assert.False(body.RootElement.TryGetProperty("transactions", out _));
    }
}
