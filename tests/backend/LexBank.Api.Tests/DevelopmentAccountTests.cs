using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LexBank.Api.Tests;

public class DevelopmentAccountTests : IDisposable
{
    private readonly LexBankApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private WebApplicationFactory<Program> StartIn(string environment) =>
        _factory.WithWebHostBuilder(builder => builder.UseEnvironment(environment));

    private static Task<HttpResponseMessage> LogInWithDevelopmentAccountAsync(HttpClient client) =>
        client.PostAsJsonAsync("/api/session", new { email = "a@a.com", password = "12345678" });

    [Fact]
    public async Task PostSession_DevelopmentAccountInDevelopment_ReturnsOk()
    {
        using var development = StartIn("Development");
        using var client = development.CreateClient();

        var response = await LogInWithDevelopmentAccountAsync(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostSession_DevelopmentAccountOutsideDevelopment_ReturnsUnauthorized()
    {
        using var production = StartIn("Production");
        using var client = production.CreateClient();

        var response = await LogInWithDevelopmentAccountAsync(client);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostSession_DevelopmentAccountAlreadyExists_StartsAgainAndReturnsOk()
    {
        using var firstStart = StartIn("Development");
        using var firstClient = firstStart.CreateClient();
        using var restarted = StartIn("Development");
        using var client = restarted.CreateClient();

        var response = await LogInWithDevelopmentAccountAsync(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
