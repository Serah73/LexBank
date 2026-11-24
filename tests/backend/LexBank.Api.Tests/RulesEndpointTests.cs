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
}
