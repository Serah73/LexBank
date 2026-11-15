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
