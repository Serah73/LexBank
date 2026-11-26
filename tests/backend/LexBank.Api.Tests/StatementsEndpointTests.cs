using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

    private async Task<HttpClient> ClientReadingWithAsync(IStatementReader reader, string email = "ana@example.com")
    {
        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IStatementReader>();
                services.AddSingleton(reader);
            })).CreateClient();
        await client.LogInAsync(email);
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
    public async Task Upload_NoRuleMatches_ReturnsUncategorizedWithoutRule()
    {
        var reader = new StubStatementReader
        {
            Transactions = [new Transaction(new DateOnly(2025, 1, 10), "tj-fauca", -4.80m, 100m, Bank.Ruralvia)],
        };
        using var client = await ClientReadingWithAsync(reader);

        var response = await client.PostAsync("/api/statements", AnyFileUpload());

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var first = body.RootElement.GetProperty("transactions")[0];
        Assert.Equal("Uncategorized", first.GetProperty("category").GetString());
        Assert.Equal(JsonValueKind.Null, first.GetProperty("rule").ValueKind);
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
        Assert.Equal(["Groceries", "Uncategorized"], categories);
    }

    [Fact]
    public async Task Upload_RuleMatches_ReturnsThePatternAndSignOfTheRule()
    {
        var reader = new StubStatementReader
        {
            Transactions = [new Transaction(new DateOnly(2025, 1, 10), "tj-mercadona finca espana", -23.40m, 100m, Bank.Ruralvia)],
        };
        using var client = await ClientReadingWithAsync(reader);

        var response = await client.PostAsync("/api/statements", AnyFileUpload());

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var rule = body.RootElement.GetProperty("transactions")[0].GetProperty("rule");
        Assert.Equal("*mercadona*", rule.GetProperty("pattern").GetString());
        Assert.Equal("Expense", rule.GetProperty("sign").GetString());
    }

    // The id of the seeded rule "*mercadona*" for expenses, found through the conflict of creating it again.
    private static async Task<int> MercadonaRuleIdAsync(HttpClient client)
    {
        var conflict = await client.PostAsJsonAsync("/api/rules", new { pattern = "*mercadona*", category = "Health", sign = "Expense" });
        using var body = JsonDocument.Parse(await conflict.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("rule").GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Upload_AfterChangingTheCategoryOfARule_CategorizesWithTheNewCategory()
    {
        var reader = new StubStatementReader
        {
            Transactions = [new Transaction(new DateOnly(2025, 1, 10), "tj-mercadona finca espana", -23.40m, 100m, Bank.Ruralvia)],
        };
        using var client = await ClientReadingWithAsync(reader);
        var id = await MercadonaRuleIdAsync(client);
        await client.PutAsJsonAsync($"/api/rules/{id}/category", new { category = "Health" });

        var response = await client.PostAsync("/api/statements", AnyFileUpload());

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var first = body.RootElement.GetProperty("transactions")[0];
        Assert.Equal("Health", first.GetProperty("category").GetString());
        Assert.Equal("Health", first.GetProperty("rule").GetProperty("category").GetString());
    }

    [Fact]
    public async Task ListTransactions_CategorizedTransaction_ReturnsItsRuleWithTheIdOfTheStoredRule()
    {
        var reader = new StubStatementReader
        {
            Transactions = [new Transaction(new DateOnly(2025, 1, 10), "tj-mercadona finca espana", -23.40m, 100m, Bank.Ruralvia)],
        };
        using var client = await ClientReadingWithAsync(reader);
        var id = await MercadonaRuleIdAsync(client);
        await client.PostAsync("/api/statements", AnyFileUpload());

        var response = await client.GetAsync("/api/transactions");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(id, body.RootElement[0].GetProperty("rule").GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task Upload_SeveralRulesMatch_ReturnsTheRuleWithTheHighestPriority()
    {
        var reader = new StubStatementReader
        {
            Transactions = [new Transaction(new DateOnly(2025, 1, 10), "cargo bizum - bocata", -5m, 100m, Bank.Ruralvia)],
        };
        using var client = await ClientReadingWithAsync(reader);

        var response = await client.PostAsync("/api/statements", AnyFileUpload());

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var first = body.RootElement.GetProperty("transactions")[0];
        Assert.Equal("bocata", first.GetProperty("rule").GetProperty("pattern").GetString());
        Assert.Equal("Restaurants", first.GetProperty("category").GetString());
    }

    [Fact]
    public async Task ListTransactions_AfterAnUpload_ReturnsTheRuleOfEachTransaction()
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
        var rules = body.RootElement.EnumerateArray().Select(t => t.GetProperty("rule"));
        Assert.Equal(["*mercadona*", null], rules.Select(r => r.ValueKind == JsonValueKind.Null ? null : r.GetProperty("pattern").GetString()));
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

    [Fact]
    public async Task Upload_NewTransactions_ReturnsTheirIds()
    {
        var reader = new StubStatementReader
        {
            Transactions =
            [
                new Transaction(new DateOnly(2025, 1, 10), "COMPRA UNO", -10m, 100m, Bank.Ruralvia),
                new Transaction(new DateOnly(2025, 1, 9), "COMPRA DOS", -5m, 110m, Bank.Ruralvia),
            ],
        };
        using var client = await ClientReadingWithAsync(reader);

        var response = await client.PostAsync("/api/statements", AnyFileUpload());
        var stored = await client.GetAsync("/api/transactions");

        using var uploaded = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var uploadedIds = uploaded.RootElement.GetProperty("transactions").EnumerateArray().Select(t => t.GetProperty("id").GetInt32()).ToArray();
        using var list = JsonDocument.Parse(await stored.Content.ReadAsStringAsync());
        var storedIds = list.RootElement.EnumerateArray().Select(t => t.GetProperty("id").GetInt32()).ToArray();
        Assert.Equal(2, uploadedIds.Distinct().Count());
        Assert.All(uploadedIds, id => Assert.True(id > 0));
        Assert.Equal(storedIds, uploadedIds);
    }

    [Fact]
    public async Task CorrectCategory_OwnTransaction_ReturnsItWithTheNewCategoryMarkedAsCorrected()
    {
        var reader = new StubStatementReader
        {
            Transactions = [new Transaction(new DateOnly(2025, 1, 10), "tj-mercadona finca espana", -23.40m, 100m, Bank.Ruralvia)],
        };
        using var client = await ClientReadingWithAsync(reader);
        var upload = await client.PostAsync("/api/statements", AnyFileUpload());
        using var uploaded = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        var id = uploaded.RootElement.GetProperty("transactions")[0].GetProperty("id").GetInt32();

        var response = await client.PutAsJsonAsync($"/api/transactions/{id}/category", new { category = "Restaurants" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(id, body.RootElement.GetProperty("transaction").GetProperty("id").GetInt32());
        Assert.Equal("Restaurants", body.RootElement.GetProperty("transaction").GetProperty("category").GetString());
        Assert.True(body.RootElement.GetProperty("transaction").GetProperty("corrected").GetBoolean());
    }

    // A client of a user with one stored transaction categorized as Groceries by the rule "*mercadona*", and its id.
    private async Task<(HttpClient Client, StubStatementReader Reader, int Id)> ClientWithOneTransactionAsync()
    {
        var reader = new StubStatementReader
        {
            Transactions = [new Transaction(new DateOnly(2025, 1, 10), "tj-mercadona finca espana", -23.40m, 100m, Bank.Ruralvia)],
        };
        var client = await ClientReadingWithAsync(reader);
        var upload = await client.PostAsync("/api/statements", AnyFileUpload());
        using var uploaded = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        return (client, reader, uploaded.RootElement.GetProperty("transactions")[0].GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task ListTransactions_AfterACorrection_ReturnsTheCorrectedCategoryAndTheEngineRule()
    {
        var (client, _, id) = await ClientWithOneTransactionAsync();
        using var _client = client;
        await client.PutAsJsonAsync($"/api/transactions/{id}/category", new { category = "Restaurants" });

        var response = await client.GetAsync("/api/transactions");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var stored = body.RootElement[0];
        Assert.Equal("Restaurants", stored.GetProperty("category").GetString());
        Assert.True(stored.GetProperty("corrected").GetBoolean());
        Assert.Equal("*mercadona*", stored.GetProperty("rule").GetProperty("pattern").GetString());
    }

    [Fact]
    public async Task CorrectCategory_ToUncategorized_IsAccepted()
    {
        var (client, _, id) = await ClientWithOneTransactionAsync();
        using var _client = client;

        var response = await client.PutAsJsonAsync($"/api/transactions/{id}/category", new { category = "Uncategorized" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Uncategorized", body.RootElement.GetProperty("transaction").GetProperty("category").GetString());
        Assert.True(body.RootElement.GetProperty("transaction").GetProperty("corrected").GetBoolean());
    }

    [Fact]
    public async Task CorrectCategory_TransactionOfAnotherUser_ReturnsNotFoundAndKeepsItsCategory()
    {
        var (client, _, id) = await ClientWithOneTransactionAsync();
        using var _client = client;
        using var other = _factory.CreateClient();
        await other.LogInAsync("luis@example.com");

        var response = await other.PutAsJsonAsync($"/api/transactions/{id}/category", new { category = "Restaurants" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var stored = await client.GetAsync("/api/transactions");
        using var body = JsonDocument.Parse(await stored.Content.ReadAsStringAsync());
        Assert.Equal("Groceries", body.RootElement[0].GetProperty("category").GetString());
        Assert.False(body.RootElement[0].GetProperty("corrected").GetBoolean());
    }

    [Fact]
    public async Task CorrectCategory_UnknownTransaction_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync("/api/transactions/999999/category", new { category = "Restaurants" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Upload_StatementWithACorrectedTransaction_SkipsItAndReturnsTheCorrectedCategory()
    {
        var (client, _, id) = await ClientWithOneTransactionAsync();
        using var _client = client;
        await client.PutAsJsonAsync($"/api/transactions/{id}/category", new { category = "Restaurants" });

        var response = await client.PostAsync("/api/statements", AnyFileUpload());
        var stored = await client.GetAsync("/api/transactions");

        using var uploaded = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, uploaded.RootElement.GetProperty("skippedTransactions").GetInt32());
        var returned = uploaded.RootElement.GetProperty("transactions")[0];
        Assert.Equal(id, returned.GetProperty("id").GetInt32());
        Assert.Equal("Restaurants", returned.GetProperty("category").GetString());
        Assert.True(returned.GetProperty("corrected").GetBoolean());
        using var list = JsonDocument.Parse(await stored.Content.ReadAsStringAsync());
        Assert.Equal(1, list.RootElement.GetArrayLength());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"category\": null}")]
    [InlineData("{\"category\": 99}")]
    [InlineData("{\"category\": \"NotACategory\"}")]
    public async Task CorrectCategory_MissingOrUnknownCategory_ReturnsBadRequestAndKeepsTheCategory(string json)
    {
        var (client, _, id) = await ClientWithOneTransactionAsync();
        using var _client = client;
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PutAsync($"/api/transactions/{id}/category", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var stored = await client.GetAsync("/api/transactions");
        using var body = JsonDocument.Parse(await stored.Content.ReadAsStringAsync());
        Assert.Equal("Groceries", body.RootElement[0].GetProperty("category").GetString());
        Assert.False(body.RootElement[0].GetProperty("corrected").GetBoolean());
    }

    [Fact]
    public async Task ApplyRules_WithoutSession_ReturnsUnauthorized()
    {
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsync("/api/transactions/apply-rules", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApplyRules_RuleCreatedAfterImport_RecategorizesTheTransactionAndReturnsTheCount()
    {
        using var client = await ClientReadingWithAsync(ReaderWithOneTransaction("zzz finca qqq"));
        await client.PostAsync("/api/statements", AnyFileUpload());
        await client.PostAsJsonAsync("/api/rules", new { pattern = "finca", category = "Restaurants", sign = "Expense" });

        var response = await client.PostAsync("/api/transactions/apply-rules", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, body.RootElement.GetProperty("changed").GetInt32());
        using var stored = JsonDocument.Parse(await client.GetStringAsync("/api/transactions"));
        var transaction = stored.RootElement[0];
        Assert.Equal("Restaurants", transaction.GetProperty("category").GetString());
        Assert.Equal("finca", transaction.GetProperty("rule").GetProperty("pattern").GetString());
        Assert.False(transaction.GetProperty("corrected").GetBoolean());
    }

    [Fact]
    public async Task ApplyRules_RuleCategoryReplaced_RecategorizesItsTransactions()
    {
        using var client = await ClientReadingWithAsync(ReaderWithOneTransaction("zzz finca qqq"));
        var created = await client.PostAsJsonAsync("/api/rules", new { pattern = "finca", category = "Restaurants", sign = "Expense" });
        using var rule = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        await client.PostAsync("/api/statements", AnyFileUpload());
        await client.PutAsJsonAsync($"/api/rules/{rule.RootElement.GetProperty("id").GetInt32()}/category", new { category = "Health" });

        var response = await client.PostAsync("/api/transactions/apply-rules", null);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, body.RootElement.GetProperty("changed").GetInt32());
        using var stored = JsonDocument.Parse(await client.GetStringAsync("/api/transactions"));
        Assert.Equal("Health", stored.RootElement[0].GetProperty("category").GetString());
    }

    [Fact]
    public async Task ApplyRules_CorrectedTransaction_KeepsTheCategoryAndUpdatesTheRule()
    {
        using var client = await ClientReadingWithAsync(ReaderWithOneTransaction("zzz finca qqq"));
        var upload = await client.PostAsync("/api/statements", AnyFileUpload());
        using var uploaded = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        var id = uploaded.RootElement.GetProperty("transactions")[0].GetProperty("id").GetInt32();
        await client.PutAsJsonAsync($"/api/transactions/{id}/category", new { category = "Health" });
        await client.PostAsJsonAsync("/api/rules", new { pattern = "finca", category = "Restaurants", sign = "Expense" });

        var response = await client.PostAsync("/api/transactions/apply-rules", null);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, body.RootElement.GetProperty("changed").GetInt32());
        using var stored = JsonDocument.Parse(await client.GetStringAsync("/api/transactions"));
        var transaction = stored.RootElement[0];
        Assert.Equal("Health", transaction.GetProperty("category").GetString());
        Assert.True(transaction.GetProperty("corrected").GetBoolean());
        Assert.Equal("finca", transaction.GetProperty("rule").GetProperty("pattern").GetString());
    }

    [Fact]
    public async Task ApplyRules_NothingChanged_ReturnsZero()
    {
        using var client = await ClientReadingWithAsync(ReaderWithOneTransaction("tj-mercadona finca espana"));
        await client.PostAsync("/api/statements", AnyFileUpload());

        var response = await client.PostAsync("/api/transactions/apply-rules", null);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, body.RootElement.GetProperty("changed").GetInt32());
    }

    [Fact]
    public async Task ApplyRules_AnotherUsersTransactions_AreNotChanged()
    {
        var reader = ReaderWithOneTransaction("zzz finca qqq");
        using var other = await ClientReadingWithAsync(reader, "luis@example.com");
        await other.PostAsync("/api/statements", AnyFileUpload());
        await _client.PostAsJsonAsync("/api/rules", new { pattern = "finca", category = "Restaurants", sign = "Expense" });

        var response = await _client.PostAsync("/api/transactions/apply-rules", null);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, body.RootElement.GetProperty("changed").GetInt32());
        using var stored = JsonDocument.Parse(await other.GetStringAsync("/api/transactions"));
        Assert.Equal("Uncategorized", stored.RootElement[0].GetProperty("category").GetString());
    }

    [Fact]
    public async Task CorrectCategory_UndefinedCategorySentAsText_ReturnsBadRequestAndChangesNothing()
    {
        using var client = await ClientReadingWithAsync(ReaderWithOneTransaction("zzz finca qqq"));
        var upload = await client.PostAsync("/api/statements", AnyFileUpload());
        using var uploaded = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        var id = uploaded.RootElement.GetProperty("transactions")[0].GetProperty("id").GetInt32();

        var response = await client.PutAsJsonAsync($"/api/transactions/{id}/category", new { category = "999" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var stored = JsonDocument.Parse(await client.GetStringAsync("/api/transactions"));
        Assert.Equal("Uncategorized", stored.RootElement[0].GetProperty("category").GetString());
    }

    [Fact]
    public async Task GetAccuracy_StoredTransactions_ReturnsHitsAndTotalOfTheSessionUserOnly()
    {
        using var other = await ClientReadingWithAsync(ReaderWithOneTransaction("tj-mercadona finca espana"), "luis@example.com");
        await other.PostAsync("/api/statements", AnyFileUpload());
        var reader = new StubStatementReader
        {
            Transactions =
            [
                new Transaction(new DateOnly(2025, 1, 10), "tj-mercadona finca espana", -23.40m, 100m, Bank.Ruralvia),
                new Transaction(new DateOnly(2025, 1, 11), "zzz finca qqq", -10m, 90m, Bank.Ruralvia),
            ],
        };
        using var client = await ClientReadingWithAsync(reader);
        await client.PostAsync("/api/statements", AnyFileUpload());

        var response = await client.GetAsync("/api/transactions/accuracy");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, body.RootElement.GetProperty("hits").GetInt32());
        Assert.Equal(2, body.RootElement.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task GetAccuracy_NoTransactions_ReturnsZeroOfZero()
    {
        var response = await _client.GetAsync("/api/transactions/accuracy");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, body.RootElement.GetProperty("hits").GetInt32());
        Assert.Equal(0, body.RootElement.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task GetAccuracy_WithoutSession_ReturnsUnauthorized()
    {
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/api/transactions/accuracy");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CorrectCategory_Saved_ReturnsTheTransactionAndTheNewAccuracy()
    {
        var (client, _, id) = await ClientWithOneTransactionAsync();
        using var _client = client;

        var response = await client.PutAsJsonAsync($"/api/transactions/{id}/category", new { category = "Restaurants" });

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Restaurants", body.RootElement.GetProperty("transaction").GetProperty("category").GetString());
        Assert.Equal(0, body.RootElement.GetProperty("accuracy").GetProperty("hits").GetInt32());
        Assert.Equal(1, body.RootElement.GetProperty("accuracy").GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task ApplyRules_RulesChanged_ReturnsTheNewAccuracy()
    {
        using var client = await ClientReadingWithAsync(ReaderWithOneTransaction("zzz finca qqq"));
        await client.PostAsync("/api/statements", AnyFileUpload());
        await client.PostAsJsonAsync("/api/rules", new { pattern = "finca", category = "Restaurants", sign = "Expense" });

        var response = await client.PostAsync("/api/transactions/apply-rules", null);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, body.RootElement.GetProperty("accuracy").GetProperty("hits").GetInt32());
        Assert.Equal(1, body.RootElement.GetProperty("accuracy").GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Upload_NewTransactions_ReturnsTheNewAccuracy()
    {
        using var first = await ClientReadingWithAsync(ReaderWithOneTransaction("tj-mercadona finca espana"));
        await first.PostAsync("/api/statements", AnyFileUpload());
        using var client = await ClientReadingWithAsync(ReaderWithOneTransaction("zzz finca qqq"));

        var response = await client.PostAsync("/api/statements", AnyFileUpload());

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, body.RootElement.GetProperty("accuracy").GetProperty("hits").GetInt32());
        Assert.Equal(2, body.RootElement.GetProperty("accuracy").GetProperty("total").GetInt32());
    }

    private static StubStatementReader ReaderWithOneTransaction(string description) => new()
    {
        Transactions = [new Transaction(new DateOnly(2025, 1, 10), description, -23.40m, 100m, Bank.Ruralvia)],
    };

    [Fact]
    public async Task Upload_AfterCreatingARule_CategorizesAMatchingTransactionWithItEvenIfASeededRuleMatches()
    {
        using var client = await ClientReadingWithAsync(ReaderWithOneTransaction("tj-mercadona finca espana"));
        await client.PostAsJsonAsync("/api/rules", new { pattern = "finca", category = "Restaurants", sign = "Expense" });

        var response = await client.PostAsync("/api/statements", AnyFileUpload());

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var first = body.RootElement.GetProperty("transactions")[0];
        Assert.Equal("Restaurants", first.GetProperty("category").GetString());
        Assert.Equal("finca", first.GetProperty("rule").GetProperty("pattern").GetString());
    }

    [Fact]
    public async Task Upload_RuleCreatedByAnotherUser_AppliesToMyImports()
    {
        using var other = _factory.CreateClient();
        await other.LogInAsync("luis@example.com");
        await other.PostAsJsonAsync("/api/rules", new { pattern = "finca", category = "Restaurants", sign = "Expense" });
        using var client = await ClientReadingWithAsync(ReaderWithOneTransaction("tj-mercadona finca espana"));

        var response = await client.PostAsync("/api/statements", AnyFileUpload());

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Restaurants", body.RootElement.GetProperty("transactions")[0].GetProperty("category").GetString());
    }

    [Fact]
    public async Task Upload_SeveralUserRulesMatch_TheNewestWins()
    {
        using var client = await ClientReadingWithAsync(ReaderWithOneTransaction("tj-mercadona finca espana"));
        await client.PostAsJsonAsync("/api/rules", new { pattern = "finca", category = "Restaurants", sign = "Expense" });
        await client.PostAsJsonAsync("/api/rules", new { pattern = "espana", category = "Health", sign = "Expense" });

        var response = await client.PostAsync("/api/statements", AnyFileUpload());

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var first = body.RootElement.GetProperty("transactions")[0];
        Assert.Equal("Health", first.GetProperty("category").GetString());
        Assert.Equal(1998, first.GetProperty("rule").GetProperty("priority").GetInt32());
    }
}
