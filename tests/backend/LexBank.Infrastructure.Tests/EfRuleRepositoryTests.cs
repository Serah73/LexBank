using LexBank.Domain;
using Microsoft.EntityFrameworkCore;

namespace LexBank.Infrastructure.Tests;

public class EfRuleRepositoryTests : IDisposable
{
    private readonly LexBankDbContext _context;

    public EfRuleRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<LexBankDbContext>()
            .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database=LexBank_Tests_{Guid.NewGuid():N};Integrated Security=true")
            .Options;
        _context = new LexBankDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Theory]
    [InlineData("tj-mercadona finca espana", -23.40, Category.Groceries)]
    [InlineData("Interés neto pagado a Cuenta Remunerada del Nov 9", 0.12, Category.Interest)]
    [InlineData("A EUR Cuenta Remunerada", -910, Category.Savings)]
    [InlineData("Desde EUR Cuenta Remunerada", 100, Category.SavingsWithdrawal)]
    [InlineData("cargo bizum - bocata", -5, Category.Restaurants)]
    [InlineData("cargo bizum - sin concepto", -20, Category.TransfersOut)]
    [InlineData("abono bizum - bizum de laura", 10, Category.TransfersIn)]
    [InlineData("NOMINA EMPRESA EJEMPLO", 1500, Category.Salary)]
    [InlineData("traspaso entre cuentas", -50, Category.Internal)]
    [InlineData("tj-alcampo la laguna", -1.5, Category.Groceries)]
    [InlineData("tj-mc donalds arrecife", -8, Category.Restaurants)]
    [InlineData("tj-zara tenerife cc meridian", -13.05, Category.Clothing)]
    [InlineData("zapatería centro", -40, Category.Footwear)]
    [InlineData("joyería ejemplo", -80, Category.Jewelry)]
    [InlineData("tj-ferreteria playa honda", -9, Category.Home)]
    [InlineData("tj-fcia matallana c.b.", -12, Category.Health)]
    [InlineData("tj-aparcamiento ramon y caja", -2, Category.Transport)]
    [InlineData("tj-vueling airlines", -60, Category.Travel)]
    [InlineData("tj-5 oceanos ejemplo", -30, Category.Groceries)]
    [InlineData("tj-bintercanarias ejemplo", -45, Category.Travel)]
    [InlineData("tj-mercadonasl ejemplo", -30, Category.Groceries)]
    [InlineData("tj-supermercados ejemplo", -30, Category.Groceries)]
    [InlineData("tj-mcdonalds ejemplo", -8, Category.Restaurants)]
    [InlineData("tj-hoteles ejemplo", -90, Category.Travel)]
    [InlineData("tj-combustibles ejemplo", -40, Category.Transport)]
    [InlineData("tj-kiwoko", -25, Category.Pets)]
    [InlineData("tj-cajero rural", -50, Category.Cash)]
    public async Task ListAsync_AfterCreatingTheDatabase_ReturnsSeededRulesThatCategorizeAsExpected(
        string description, double amount, Category expected)
    {
        var repository = new EfRuleRepository(_context);

        var rules = await repository.ListAsync();
        var category = new Categorizer(rules).Categorize(description, (decimal)amount).Category;

        Assert.Equal(expected, category);
    }

    [Fact]
    public async Task ListAsync_SeededRules_ReturnsThemWithTheIdsOfInitialRules()
    {
        var rules = await new EfRuleRepository(_context).ListAsync();

        Assert.Equal(
            InitialRules.All.Select(r => (r.Id, r.Rule.Pattern)).OrderBy(r => r.Id),
            rules.Select(r => (r.Id, r.Pattern)).OrderBy(r => r.Id));
    }

    [Fact]
    public async Task ListAsync_RulesWithSamePriority_ReturnsThemByPriorityThenId()
    {
        var repository = new EfRuleRepository(_context);
        await repository.AddAsync(new Rule("tie-b", Category.Health, Priority: 5));
        await repository.AddAsync(new Rule("first", Category.Health, Priority: 1));
        await repository.AddAsync(new Rule("tie-a", Category.Health, Priority: 5));

        var rules = await repository.ListAsync();

        Assert.Equal(["first", "tie-b", "tie-a"], rules.Take(3).Select(r => r.Pattern));
    }

    [Fact]
    public async Task UpdateAsync_RuleWithAnotherCategory_ChangesItInPlaceWithoutCreatingARule()
    {
        var repository = new EfRuleRepository(_context);
        var before = await repository.ListAsync();
        var seeded = before.First(r => r.Pattern == "*mercadona*");

        await repository.UpdateAsync(seeded with { Category = Category.Health });

        var after = await repository.ListAsync();
        Assert.Equal(before.Count, after.Count);
        var changed = Assert.Single(after, r => r.Id == seeded.Id);
        Assert.Equal(seeded with { Category = Category.Health }, changed);
    }
}
