using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using LexBank.Domain;
using LexBank.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LexBank.Api.Tests;

public class SessionEndpointTests : IDisposable
{
    private readonly LexBankApiFactory _factory = new();
    private readonly HttpClient _client;

    public SessionEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task PostSession_ValidCredentials_ReturnsOkAndSetsSessionCookie()
    {
        await _client.PostAsJsonAsync("/api/users", new { email = "ana@example.com", password = "a-long-password" });

        var response = await _client.PostAsJsonAsync("/api/session", new { email = "ana@example.com", password = "a-long-password" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.NotEmpty(cookies);
    }

    [Fact]
    public async Task PostSession_ValidCredentials_SessionCookieIsHttpOnlyAndSameSiteStrict()
    {
        await _client.PostAsJsonAsync("/api/users", new { email = "ana@example.com", password = "a-long-password" });

        var response = await _client.PostAsJsonAsync("/api/session", new { email = "ana@example.com", password = "a-long-password" });

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostSession_WrongPassword_ReturnsUnauthorizedWithInvalidCredentials()
    {
        await _client.PostAsJsonAsync("/api/users", new { email = "ana@example.com", password = "a-long-password" });

        var response = await _client.PostAsJsonAsync("/api/session", new { email = "ana@example.com", password = "not-the-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("InvalidCredentials", body.RootElement.GetProperty("error").GetString());
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task PostSession_UnknownEmail_ReturnsUnauthorizedWithInvalidCredentials()
    {
        await _client.PostAsJsonAsync("/api/users", new { email = "ana@example.com", password = "a-long-password" });

        var response = await _client.PostAsJsonAsync("/api/session", new { email = "luis@example.com", password = "a-long-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("InvalidCredentials", body.RootElement.GetProperty("error").GetString());
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task PostSession_EmailInDifferentCase_ReturnsOk()
    {
        await _client.PostAsJsonAsync("/api/users", new { email = "ana@example.com", password = "a-long-password" });

        var response = await _client.PostAsJsonAsync("/api/session", new { email = "ANA@Example.COM", password = "a-long-password" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteSession_WithSession_NextRequestIsUnauthorized()
    {
        await _client.LogInAsync();

        await _client.DeleteAsync("/api/session");
        var response = await _client.GetAsync("/api/transactions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTransactions_SessionOfAUserThatNoLongerExists_ReturnsUnauthorized()
    {
        await _client.LogInAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<LexBankDbContext>();
            await context.Set<User>().ExecuteDeleteAsync();
        }

        var response = await _client.GetAsync("/api/transactions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostSession_EmailInDifferentCase_StoresTheRegisteredEmailInTheSession()
    {
        await _client.PostAsJsonAsync("/api/users", new { email = "ana@example.com", password = "a-long-password" });

        var response = await _client.PostAsJsonAsync("/api/session", new { email = "ANA@Example.COM", password = "a-long-password" });

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        var value = cookie.Split(';')[0].Split('=', 2)[1];
        var options = _factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var ticket = options.TicketDataFormat.Unprotect(value);
        Assert.Equal("ana@example.com", ticket!.Principal.FindFirstValue(ClaimTypes.Email));
    }

    [Fact]
    public async Task GetTransactions_WithoutSession_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/transactions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostStatements_WithoutSession_ReturnsUnauthorized()
    {
        using var content = new MultipartFormDataContent
        {
            { new ByteArrayContent("any content"u8.ToArray()), "file", "statement.xlsx" },
        };

        var response = await _client.PostAsync("/api/statements", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
