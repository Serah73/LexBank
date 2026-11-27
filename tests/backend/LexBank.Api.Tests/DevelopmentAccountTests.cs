using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;

namespace LexBank.Api.Tests;

public class DevelopmentAccountTests : IDisposable
{
    private readonly LexBankApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static Task<HttpResponseMessage> LogInWithDevelopmentAccountAsync(HttpClient client) =>
        client.PostAsJsonAsync("/api/session", new { email = "a@a.com", password = "12345678" });

    [Fact]
    public async Task PostSession_DevelopmentAccountInDevelopment_ReturnsOk()
    {
        using var client = _factory.CreateClient();

        var response = await LogInWithDevelopmentAccountAsync(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostSession_DevelopmentAccountOutsideDevelopment_ReturnsUnauthorized()
    {
        using var production = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = production.CreateClient();

        var response = await LogInWithDevelopmentAccountAsync(client);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
