using LexBank.Application;
using LexBank.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IStatementReader, RuralviaXlsxReader>();

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
    catch (UnsupportedStatementException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
}).DisableAntiforgery();

app.Run();

public partial class Program { }
