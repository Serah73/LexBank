using System.Text.Json.Serialization;
using LexBank.Application;
using LexBank.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IStatementReader, RuralviaXlsxReader>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/api/statements", (IFormFile file, IStatementReader reader) =>
{
    using var stream = file.OpenReadStream();
    try
    {
        return Results.Ok(reader.Read(stream));
    }
    catch (UnsupportedStatementException)
    {
        return Results.BadRequest(new { error = "NotRuralviaStatement" });
    }
}).DisableAntiforgery();

app.Run();

public partial class Program { }
