using System.Security.Claims;
using System.Text.Json.Serialization;
using LexBank.Application;
using LexBank.Domain;
using LexBank.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IStatementReader, RuralviaXlsxReader>();
builder.Services.AddScoped<IStatementReader, RevolutCsvReader>();
builder.Services.AddDbContext<LexBankDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LexBank")));
builder.Services.AddScoped<ITransactionRepository, EfTransactionRepository>();
builder.Services.AddScoped<IRuleRepository, EfRuleRepository>();
builder.Services.AddScoped<UploadStatement>();
builder.Services.AddScoped<CorrectCategory>();
builder.Services.AddScoped<CreateRule>();
builder.Services.AddScoped<ChangeRuleCategory>();
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
builder.Services.AddScoped<SignUp>();
builder.Services.AddScoped<LogIn>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnValidatePrincipal = async context =>
    {
        var email = context.Principal?.FindFirstValue(ClaimTypes.Email);
        var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        if (email is null || !await users.ExistsAsync(email))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

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
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/statements", async (IFormFile file, ClaimsPrincipal user, UploadStatement uploadStatement) =>
{
    using var stream = file.OpenReadStream();
    try
    {
        return Results.Ok(await uploadStatement.ExecuteAsync(user.FindFirstValue(ClaimTypes.Email)!, file.FileName, stream));
    }
    catch (UnsupportedStatementException)
    {
        return Results.BadRequest(new { error = "UnsupportedStatement" });
    }
}).DisableAntiforgery().RequireAuthorization();

app.MapGet("/api/transactions", async (ClaimsPrincipal user, ITransactionRepository repository) =>
    Results.Ok(await repository.ListAsync(user.FindFirstValue(ClaimTypes.Email)!))).RequireAuthorization();

app.MapPut("/api/transactions/{id:int}/category", async (int id, CorrectCategoryRequest request, ClaimsPrincipal user, CorrectCategory correctCategory) =>
{
    if (request.Category is null) return Results.BadRequest();
    try
    {
        return Results.Ok(await correctCategory.ExecuteAsync(user.FindFirstValue(ClaimTypes.Email)!, id, request.Category.Value));
    }
    catch (TransactionNotFoundException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization();

app.MapGet("/api/rules", async (IRuleRepository repository) =>
    Results.Ok((await repository.ListAsync()).Select(r => new RuleResponse(r.Pattern, r.Sign, r.Category)))).RequireAuthorization();

app.MapPost("/api/rules", async (CreateRuleRequest request, CreateRule createRule) =>
{
    if (request.Category is null or Category.Uncategorized || request.Sign is null) return Results.BadRequest();
    try
    {
        return Results.Created((string?)null, await createRule.ExecuteAsync(request.Pattern ?? string.Empty, request.Category.Value, request.Sign.Value));
    }
    catch (InvalidRulePatternException)
    {
        return Results.BadRequest(new { error = "InvalidRulePattern" });
    }
    catch (RuleAlreadyExistsException exception)
    {
        return Results.Conflict(new { error = "RuleAlreadyExists", rule = exception.Existing });
    }
}).RequireAuthorization();

app.MapPut("/api/rules/{id:int}/category", async (int id, ChangeRuleCategoryRequest request, ChangeRuleCategory changeRuleCategory) =>
{
    if (request.Category is null or Category.Uncategorized) return Results.BadRequest();
    try
    {
        return Results.Ok(await changeRuleCategory.ExecuteAsync(id, request.Category.Value));
    }
    catch (RuleNotFoundException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization();

app.MapPost("/api/users",async (SignUpRequest request, SignUp signUp) =>
{
    try
    {
        await signUp.ExecuteAsync(request.Email ?? string.Empty, request.Password ?? string.Empty);
        return Results.Created();
    }
    catch (InvalidEmailException)
    {
        return Results.BadRequest(new { error = "InvalidEmail" });
    }
    catch (InvalidPasswordException)
    {
        return Results.BadRequest(new { error = "InvalidPassword" });
    }
    catch (EmailAlreadyRegisteredException)
    {
        return Results.Conflict(new { error = "EmailAlreadyRegistered" });
    }
});

app.MapPost("/api/session", async (LogInRequest request, LogIn logIn, HttpContext context) =>
{
    User user;
    try
    {
        user = await logIn.ExecuteAsync(request.Email ?? string.Empty, request.Password ?? string.Empty);
    }
    catch (InvalidCredentialsException)
    {
        return Results.Json(new { error = "InvalidCredentials" }, statusCode: StatusCodes.Status401Unauthorized);
    }

    var identity = new ClaimsIdentity(
        [new Claim(ClaimTypes.Email, user.Email)],
        CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(new ClaimsPrincipal(identity));
    return Results.Ok();
});

app.MapDelete("/api/session", async (HttpContext context) =>
{
    await context.SignOutAsync();
    return Results.NoContent();
});

app.Run();

public record SignUpRequest(string? Email, string? Password);

public record CorrectCategoryRequest(Category? Category);

public record ChangeRuleCategoryRequest(Category? Category);

public record CreateRuleRequest(string? Pattern, Category? Category, AmountSign? Sign);

public record LogInRequest(string? Email, string? Password);

public record RuleResponse(string Pattern, AmountSign Sign, Category Category);

public partial class Program { }
