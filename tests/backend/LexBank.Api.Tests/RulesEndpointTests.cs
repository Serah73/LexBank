using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LexBank.Api.Tests;

public class RulesEndpointTests : IAsyncLifetime
{
    private readonly LexBankApiFactory _factory = new();
    private readonly HttpClient _client;

    public RulesEndpointTests()
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

    [Fact]
    public async Task CreateRule_ValidRule_ReturnsCreatedWithTheRuleAndTheNewestPriority()
    {
        var response = await _client.PostAsJsonAsync("/api/rules", new { pattern = "finca", category = "Restaurants", sign = "Expense" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("finca", body.RootElement.GetProperty("pattern").GetString());
        Assert.Equal("Restaurants", body.RootElement.GetProperty("category").GetString());
        Assert.Equal("Expense", body.RootElement.GetProperty("sign").GetString());
        Assert.Equal(1999, body.RootElement.GetProperty("priority").GetInt32());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("*")]
    [InlineData("**foo")]
    [InlineData("foo**")]
    public async Task CreateRule_InvalidPattern_ReturnsBadRequestWithInvalidRulePatternAndCreatesNothing(string pattern)
    {
        var response = await _client.PostAsJsonAsync("/api/rules", new { pattern, category = "Restaurants", sign = "Expense" });
        var next = await _client.PostAsJsonAsync("/api/rules", new { pattern = "finca", category = "Restaurants", sign = "Expense" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("InvalidRulePattern", body.RootElement.GetProperty("error").GetString());
        using var nextBody = JsonDocument.Parse(await next.Content.ReadAsStringAsync());
        Assert.Equal(1999, nextBody.RootElement.GetProperty("priority").GetInt32());
    }

    [Fact]
    public async Task CreateRule_SamePatternAndSignAlreadyExists_ReturnsConflictWithRuleAlreadyExists()
    {
        await _client.PostAsJsonAsync("/api/rules", new { pattern = "finca", category = "Restaurants", sign = "Expense" });

        var seeded = await _client.PostAsJsonAsync("/api/rules", new { pattern = "*mercadona*", category = "Restaurants", sign = "Expense" });
        var created = await _client.PostAsJsonAsync("/api/rules", new { pattern = "finca", category = "Health", sign = "Expense" });
        var otherCase = await _client.PostAsJsonAsync("/api/rules", new { pattern = "FINCA", category = "Health", sign = "Expense" });

        foreach (var response in new[] { seeded, created, otherCase })
        {
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("RuleAlreadyExists", body.RootElement.GetProperty("error").GetString());
        }
    }

    [Fact]
    public async Task CreateRule_SamePatternAndSignAlreadyExistsWithAnotherCategory_ReturnsConflictWithTheExistingRule()
    {
        var first = await _client.PostAsJsonAsync("/api/rules", new { pattern = "finca", category = "Restaurants", sign = "Expense" });
        using var firstBody = JsonDocument.Parse(await first.Content.ReadAsStringAsync());

        var userRule = await _client.PostAsJsonAsync("/api/rules", new { pattern = "finca", category = "Health", sign = "Expense" });
        var seededRule = await _client.PostAsJsonAsync("/api/rules", new { pattern = "*mercadona*", category = "Health", sign = "Expense" });

        using var userRuleBody = JsonDocument.Parse(await userRule.Content.ReadAsStringAsync());
        var existing = userRuleBody.RootElement.GetProperty("rule");
        Assert.Equal(firstBody.RootElement.GetProperty("id").GetInt32(), existing.GetProperty("id").GetInt32());
        Assert.Equal("finca", existing.GetProperty("pattern").GetString());
        Assert.Equal("Restaurants", existing.GetProperty("category").GetString());
        Assert.Equal("Expense", existing.GetProperty("sign").GetString());
        Assert.Equal(1999, existing.GetProperty("priority").GetInt32());

        using var seededRuleBody = JsonDocument.Parse(await seededRule.Content.ReadAsStringAsync());
        var seeded = seededRuleBody.RootElement.GetProperty("rule");
        Assert.Equal("*mercadona*", seeded.GetProperty("pattern").GetString());
        Assert.Equal("Groceries", seeded.GetProperty("category").GetString());
        Assert.Equal("Expense", seeded.GetProperty("sign").GetString());
        Assert.Equal(2006, seeded.GetProperty("priority").GetInt32());
        Assert.True(seeded.GetProperty("id").GetInt32() > 0);
    }

    [Fact]
    public async Task ChangeRuleCategory_UserRule_ReturnsOkWithTheNewCategoryAndKeepsPatternSignAndPriority()
    {
        var created = await _client.PostAsJsonAsync("/api/rules", new { pattern = "finca", category = "Restaurants", sign = "Expense" });
        using var createdBody = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = createdBody.RootElement.GetProperty("id").GetInt32();

        var response = await _client.PutAsJsonAsync($"/api/rules/{id}/category", new { category = "Health" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(id, body.RootElement.GetProperty("id").GetInt32());
        Assert.Equal("finca", body.RootElement.GetProperty("pattern").GetString());
        Assert.Equal("Health", body.RootElement.GetProperty("category").GetString());
        Assert.Equal("Expense", body.RootElement.GetProperty("sign").GetString());
        Assert.Equal(1999, body.RootElement.GetProperty("priority").GetInt32());
    }

    [Fact]
    public async Task ChangeRuleCategory_SeededRule_ReturnsOkWithTheNewCategory()
    {
        var conflict = await _client.PostAsJsonAsync("/api/rules", new { pattern = "*mercadona*", category = "Health", sign = "Expense" });
        using var conflictBody = JsonDocument.Parse(await conflict.Content.ReadAsStringAsync());
        var id = conflictBody.RootElement.GetProperty("rule").GetProperty("id").GetInt32();

        var response = await _client.PutAsJsonAsync($"/api/rules/{id}/category", new { category = "Health" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("*mercadona*", body.RootElement.GetProperty("pattern").GetString());
        Assert.Equal("Health", body.RootElement.GetProperty("category").GetString());
        Assert.Equal(2006, body.RootElement.GetProperty("priority").GetInt32());
    }

    [Fact]
    public async Task ChangeRuleCategory_UnknownId_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync("/api/rules/999999/category", new { category = "Health" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"category\": \"Uncategorized\"}")]
    [InlineData("{\"category\": \"NotACategory\"}")]
    [InlineData("{\"category\": 1}")]
    public async Task ChangeRuleCategory_UncategorizedMissingOrUnknownCategory_ReturnsBadRequestAndChangesNothing(string json)
    {
        var created = await _client.PostAsJsonAsync("/api/rules", new { pattern = "finca", category = "Restaurants", sign = "Expense" });
        using var createdBody = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = createdBody.RootElement.GetProperty("id").GetInt32();
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PutAsync($"/api/rules/{id}/category", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var again = await _client.PostAsJsonAsync("/api/rules", new { pattern = "finca", category = "Health", sign = "Expense" });
        using var againBody = JsonDocument.Parse(await again.Content.ReadAsStringAsync());
        Assert.Equal("Restaurants", againBody.RootElement.GetProperty("rule").GetProperty("category").GetString());
    }

    [Fact]
    public async Task ChangeRuleCategory_WithoutSession_ReturnsUnauthorized()
    {
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.PutAsJsonAsync("/api/rules/1/category", new { category = "Health" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateRule_SamePatternWithAnotherSign_IsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/rules", new { pattern = "*mercadona*", category = "Restaurants", sign = "Income" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("{\"category\": \"Restaurants\", \"sign\": \"Expense\"}")]
    [InlineData("{\"pattern\": \"finca\", \"sign\": \"Expense\"}")]
    [InlineData("{\"pattern\": \"finca\", \"category\": \"Restaurants\"}")]
    [InlineData("{\"pattern\": \"finca\", \"category\": \"NotACategory\", \"sign\": \"Expense\"}")]
    [InlineData("{\"pattern\": \"finca\", \"category\": \"Restaurants\", \"sign\": 1}")]
    public async Task CreateRule_MissingOrUnknownCategoryOrSign_ReturnsBadRequest(string json)
    {
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/rules", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateRule_WithoutSession_ReturnsUnauthorized()
    {
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("/api/rules", new { pattern = "finca", category = "Restaurants", sign = "Expense" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateRule_PatternLongerThanTheMaximum_ReturnsBadRequestWithInvalidRulePattern()
    {
        var response = await _client.PostAsJsonAsync("/api/rules", new { pattern = new string('a', 101), category = "Restaurants", sign = "Expense" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("InvalidRulePattern", body.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task CreateRule_PatternOfTheMaximumLength_IsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/rules", new { pattern = new string('a', 100), category = "Restaurants", sign = "Expense" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateRule_UncategorizedCategory_ReturnsBadRequestAndCreatesNothing()
    {
        var response = await _client.PostAsJsonAsync("/api/rules", new { pattern = "finca", category = "Uncategorized", sign = "Expense" });
        var next = await _client.PostAsJsonAsync("/api/rules", new { pattern = "finca", category = "Restaurants", sign = "Expense" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.Created, next.StatusCode);
    }
}
