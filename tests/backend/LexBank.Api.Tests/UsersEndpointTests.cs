using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LexBank.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LexBank.Api.Tests;

public class UsersEndpointTests : IDisposable
{
    private readonly LexBankApiFactory _factory = new();
    private readonly HttpClient _client;

    public UsersEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task SignUp_NewEmail_CreatesTheAccount()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new { email = "ana@example.com", password = "a-long-password" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task SignUp_PasswordOfExactlyEightCharacters_CreatesTheAccount()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new { email = "ana@example.com", password = "12345678" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task SignUp_EmailAlreadyRegistered_RejectsWithEmailAlreadyInUse()
    {
        await _client.PostAsJsonAsync("/api/users", new { email = "ana@example.com", password = "a-long-password" });

        var response = await _client.PostAsJsonAsync("/api/users", new { email = "ana@example.com", password = "another-password" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("EmailAlreadyRegistered", body.RootElement.GetProperty("error").GetString());
        Assert.Equal(1, await CountStoredUsersAsync());
    }

    [Fact]
    public async Task SignUp_EmailAlreadyRegisteredWithDifferentLetterCase_RejectsWithEmailAlreadyInUse()
    {
        await _client.PostAsJsonAsync("/api/users", new { email = "ana@example.com", password = "a-long-password" });

        var response = await _client.PostAsJsonAsync("/api/users", new { email = "ANA@Example.COM", password = "another-password" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("EmailAlreadyRegistered", body.RootElement.GetProperty("error").GetString());
        Assert.Equal(1, await CountStoredUsersAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ana.example.com")]
    [InlineData("ana@")]
    [InlineData("@example.com")]
    public async Task SignUp_InvalidEmail_RejectsWithInvalidEmail(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/users", new { email, password = "a-long-password" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("InvalidEmail", body.RootElement.GetProperty("error").GetString());
        Assert.Equal(0, await CountStoredUsersAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567")]
    public async Task SignUp_PasswordShorterThanEightCharacters_RejectsWithInvalidPassword(string password)
    {
        var response = await _client.PostAsJsonAsync("/api/users", new { email = "ana@example.com", password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("InvalidPassword", body.RootElement.GetProperty("error").GetString());
        Assert.Equal(0, await CountStoredUsersAsync());
    }

    [Theory]
    [InlineData("""{ "password": "a-long-password" }""", "InvalidEmail")]
    [InlineData("""{ "email": "ana@example.com" }""", "InvalidPassword")]
    public async Task SignUp_MissingEmailOrPassword_RejectsWithTheMatchingCode(string json, string expectedCode)
    {
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/users", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedCode, body.RootElement.GetProperty("error").GetString());
        Assert.Equal(0, await CountStoredUsersAsync());
    }

    [Fact]
    public async Task SignUp_NewEmail_StoresThePasswordProtectedNotInPlainText()
    {
        const string password = "a-long-password";

        await _client.PostAsJsonAsync("/api/users", new { email = "ana@example.com", password });

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LexBankDbContext>();
        var storedHashes = await context.Database
            .SqlQuery<string>($"SELECT PasswordHash AS Value FROM Users")
            .ToListAsync();

        var storedHash = Assert.Single(storedHashes);
        Assert.NotEmpty(storedHash);
        Assert.DoesNotContain(password, storedHash);
    }

    private async Task<int> CountStoredUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LexBankDbContext>();
        var counts = await context.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM Users").ToListAsync();
        return Assert.Single(counts);
    }
}
