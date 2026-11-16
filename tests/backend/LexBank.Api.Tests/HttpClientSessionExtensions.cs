using System.Net.Http.Json;

namespace LexBank.Api.Tests;

public static class HttpClientSessionExtensions
{
    private const string Password = "a-long-password";

    // Signs up the user if the email is not registered yet and logs in, so the client keeps the session cookie.
    public static async Task LogInAsync(this HttpClient client, string email = "ana@example.com")
    {
        await client.PostAsJsonAsync("/api/users", new { email, password = Password });
        var response = await client.PostAsJsonAsync("/api/session", new { email, password = Password });
        response.EnsureSuccessStatusCode();
    }
}
