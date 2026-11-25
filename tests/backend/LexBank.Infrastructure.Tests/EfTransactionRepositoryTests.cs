using LexBank.Domain;
using Microsoft.EntityFrameworkCore;

namespace LexBank.Infrastructure.Tests;

public class EfTransactionRepositoryTests : IDisposable
{
    private const string Email = "ana@example.com";

    private readonly LexBankDbContext _context;

    public EfTransactionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<LexBankDbContext>()
            .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database=LexBank_Tests_{Guid.NewGuid():N};Integrated Security=true")
            .Options;
        _context = new LexBankDbContext(options);
        _context.Database.EnsureCreated();
        _context.Set<User>().Add(new User(Email, "hash"));
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task ListAsync_WithNothingStored_ReturnsEmptyList()
    {
        var repository = new EfTransactionRepository(_context);

        var stored = await repository.ListAsync(Email);

        Assert.Empty(stored);
    }

    [Fact]
    public async Task ListAsync_AfterAddingTransactionsInSeveralBatches_ReturnsThemByDateDescendingAndSameDateInTheOrderTheyWereAdded()
    {
        var repository = new EfTransactionRepository(_context);

        await repository.AddAsync(Email, [new Transaction(new DateOnly(2025, 1, 5), "ENERO", 10.00m, 100.00m, Bank.Ruralvia)]);
        await repository.AddAsync(Email,
        [
            new Transaction(new DateOnly(2025, 3, 5), "MARZO PRIMERO", 20.00m, 120.00m, Bank.Ruralvia),
            new Transaction(new DateOnly(2025, 3, 5), "MARZO SEGUNDO", 5.00m, 125.00m, Bank.Ruralvia),
        ]);
        await repository.AddAsync(Email, [new Transaction(new DateOnly(2025, 2, 5), "FEBRERO", 30.00m, 90.00m, Bank.Ruralvia)]);
        var stored = await repository.ListAsync(Email);

        Assert.Equal(["MARZO PRIMERO", "MARZO SEGUNDO", "FEBRERO", "ENERO"], stored.Select(t => t.Description));
    }

    [Fact]
    public async Task ListAsync_AfterAddingTransactions_ReturnsThemWithSameDateDescriptionAmountAndBalance()
    {
        var repository = new EfTransactionRepository(_context);

        await repository.AddAsync(Email,
        [
            new Transaction(new DateOnly(2025, 1, 10), "COMPRA SUPERMERCADO EJEMPLO", -23.45m, 1000.00m, Bank.Ruralvia),
            new Transaction(new DateOnly(2025, 1, 11), "NOMINA EMPRESA EJEMPLO", 1500.00m, 2476.55m, Bank.Ruralvia),
        ]);
        var stored = await repository.ListAsync(Email);

        Assert.Equal(
            new[]
            {
                new Transaction(new DateOnly(2025, 1, 11), "NOMINA EMPRESA EJEMPLO", 1500.00m, 2476.55m, Bank.Ruralvia),
                new Transaction(new DateOnly(2025, 1, 10), "COMPRA SUPERMERCADO EJEMPLO", -23.45m, 1000.00m, Bank.Ruralvia),
            },
            stored.Select(t => t with { Id = 0 }));
    }

    [Fact]
    public async Task ListAsync_StoredTransactionsFromDifferentBanks_KeepTheirBank()
    {
        var repository = new EfTransactionRepository(_context);

        await repository.AddAsync(Email,
        [
            new Transaction(new DateOnly(2025, 1, 10), "DESDE RURALVIA", -10.00m, 100.00m, Bank.Ruralvia),
            new Transaction(new DateOnly(2025, 1, 9), "DESDE REVOLUT", -5.00m, 110.00m, Bank.Revolut),
        ]);
        var stored = await repository.ListAsync(Email);

        Assert.Equal([Bank.Ruralvia, Bank.Revolut], stored.Select(t => t.Bank));
    }

    [Fact]
    public async Task ListAsync_StoredTransactionsCategorizedAndUncategorized_KeepTheirCategory()
    {
        var repository = new EfTransactionRepository(_context);

        await repository.AddAsync(Email,
        [
            new Transaction(new DateOnly(2025, 1, 10), "COMPRA MERCADONA", -10.00m, 100.00m, Bank.Ruralvia, Category.Groceries),
            new Transaction(new DateOnly(2025, 1, 9), "SIN REGLA", -5.00m, 110.00m, Bank.Ruralvia),
        ]);
        var stored = await repository.ListAsync(Email);

        Assert.Equal([Category.Groceries, Category.Uncategorized], stored.Select(t => t.Category));
    }

    [Fact]
    public async Task ListAsync_StoredTransactionCategorizedByARule_ReturnsThatRuleWithoutDuplicatingIt()
    {
        var repository = new EfTransactionRepository(_context);
        var rules = await new EfRuleRepository(_context).ListAsync();
        var rule = rules.Single(r => r.Pattern == "*mercadona*");

        await repository.AddAsync(Email,
        [
            new Transaction(new DateOnly(2025, 1, 10), "COMPRA MERCADONA", -10.00m, 100.00m, Bank.Ruralvia, Category.Groceries) { Rule = rule },
        ]);
        var stored = await repository.ListAsync(Email);

        Assert.Equal(rule, stored.Single().Rule);
        Assert.Equal(rules.Count, await _context.Set<Rule>().CountAsync());
    }

    [Fact]
    public async Task AddAsync_RuleChangedAfterItWasRead_StoresTheTransactionWithTheStoredRule()
    {
        var repository = new EfTransactionRepository(_context);
        var rules = await new EfRuleRepository(_context).ListAsync();
        var staleRule = rules.Single(r => r.Pattern == "*mercadona*");
        await new EfRuleRepository(_context).UpdateAsync(staleRule with { Category = Category.Health });

        await repository.AddAsync(Email,
        [
            new Transaction(new DateOnly(2025, 1, 10), "COMPRA MERCADONA", -10.00m, 100.00m, Bank.Ruralvia, Category.Groceries) { Rule = staleRule },
        ]);
        var stored = await repository.ListAsync(Email);

        Assert.Equal(staleRule.Id, stored.Single().Rule!.Id);
        Assert.Equal(Category.Health, stored.Single().Rule!.Category);
        Assert.Equal(rules.Count, await _context.Set<Rule>().CountAsync());
    }

    [Fact]
    public async Task UpdateAsync_TransactionWithAnotherRule_StoresTheNewRule()
    {
        var repository = new EfTransactionRepository(_context);
        var rules = await new EfRuleRepository(_context).ListAsync();
        var oldRule = rules.Single(r => r.Pattern == "*mercadona*");
        var newRule = rules.First(r => r.Id != oldRule.Id);
        await repository.AddAsync(Email,
        [
            new Transaction(new DateOnly(2025, 1, 10), "COMPRA MERCADONA", -10.00m, 100.00m, Bank.Ruralvia, Category.Groceries) { Rule = oldRule },
        ]);
        var stored = (await repository.ListAsync(Email)).Single();

        await repository.UpdateAsync(Email, stored with { Rule = newRule });

        Assert.Equal(newRule.Id, (await repository.ListAsync(Email)).Single().Rule!.Id);
    }

    [Fact]
    public async Task ListAsync_StoredTransactionWithoutRule_ReturnsNoRule()
    {
        var repository = new EfTransactionRepository(_context);

        await repository.AddAsync(Email, [new Transaction(new DateOnly(2025, 1, 9), "SIN REGLA", -5.00m, 110.00m, Bank.Ruralvia)]);
        var stored = await repository.ListAsync(Email);

        Assert.Null(stored.Single().Rule);
    }
}
