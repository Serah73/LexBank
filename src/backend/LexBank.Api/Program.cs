using System.Text.Json.Serialization;
using LexBank.Application;
using LexBank.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IStatementReader, RuralviaXlsxReader>();
builder.Services.AddDbContext<LexBankDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LexBank")));
builder.Services.AddScoped<ITransactionRepository, EfTransactionRepository>();
builder.Services.AddScoped<UploadStatement>();
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
builder.Services.AddScoped<SignUp>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<LexBankDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/api/statements", async (IFormFile file, UploadStatement uploadStatement) =>
{
    using var stream = file.OpenReadStream();
    try
    {
        return Results.Ok(await uploadStatement.ExecuteAsync(stream));
    }
    catch (UnsupportedStatementException)
    {
        return Results.BadRequest(new { error = "NotRuralviaStatement" });
    }
}).DisableAntiforgery();

app.MapGet("/api/transactions", async (ITransactionRepository repository) =>
    Results.Ok(await repository.ListAsync()));

app.MapPost("/api/users", async (SignUpRequest request, SignUp signUp) =>
{
    try
    {
        await signUp.ExecuteAsync(request.Email, request.Password);
        return Results.Created();
    }
    catch (EmailAlreadyRegisteredException)
    {
        return Results.Conflict(new { error = "EmailAlreadyRegistered" });
    }
});

app.Run();

public record SignUpRequest(string Email, string Password);

public partial class Program { }
