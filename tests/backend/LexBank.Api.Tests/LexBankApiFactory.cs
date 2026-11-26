using LexBank.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace LexBank.Api.Tests;

public class LexBankApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString =
        $"Server=(localdb)\\MSSQLLocalDB;Database=LexBank_ApiTests_{Guid.NewGuid():N};Integrated Security=true";

    // The tests run in Development, which logs every SQL command; it would flood the test output.
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:LexBank", _connectionString)
            .UseSetting("Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command", "Warning");

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        var options = new DbContextOptionsBuilder<LexBankDbContext>().UseSqlServer(_connectionString).Options;
        using var context = new LexBankDbContext(options);
        context.Database.EnsureDeleted();
    }
}
