using LexBank.Domain;
using Microsoft.EntityFrameworkCore;

namespace LexBank.Infrastructure.Tests;

public class EfTransactionRepositoryTests : IDisposable
{
    private readonly LexBankDbContext _context;

    public EfTransactionRepositoryTests()
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

    [Fact]
    public async Task ListAsync_WithNothingStored_ReturnsEmptyList()
    {
        var repository = new EfTransactionRepository(_context);

        var stored = await repository.ListAsync();

        Assert.Empty(stored);
    }

    [Fact]
    public async Task ListAsync_AfterAddingTransactionsInSeveralBatches_ReturnsThemByDateDescendingAndSameDateInTheOrderTheyWereAdded()
    {
        var repository = new EfTransactionRepository(_context);

        await repository.AddAsync([new Transaction(new DateOnly(2025, 1, 5), "ENERO", 10.00m, 100.00m)]);
        await repository.AddAsync(
        [
            new Transaction(new DateOnly(2025, 3, 5), "MARZO PRIMERO", 20.00m, 120.00m),
            new Transaction(new DateOnly(2025, 3, 5), "MARZO SEGUNDO", 5.00m, 125.00m),
        ]);
        await repository.AddAsync([new Transaction(new DateOnly(2025, 2, 5), "FEBRERO", 30.00m, 90.00m)]);
        var stored = await repository.ListAsync();

        Assert.Equal(["MARZO PRIMERO", "MARZO SEGUNDO", "FEBRERO", "ENERO"], stored.Select(t => t.Description));
    }

    [Fact]
    public async Task ListAsync_AfterAddingTransactions_ReturnsThemWithSameDateDescriptionAmountAndBalance()
    {
        var repository = new EfTransactionRepository(_context);

        await repository.AddAsync(
        [
            new Transaction(new DateOnly(2025, 1, 10), "COMPRA SUPERMERCADO EJEMPLO", -23.45m, 1000.00m),
            new Transaction(new DateOnly(2025, 1, 11), "NOMINA EMPRESA EJEMPLO", 1500.00m, 2476.55m),
        ]);
        var stored = await repository.ListAsync();

        Assert.Equal(
            new[]
            {
                new Transaction(new DateOnly(2025, 1, 11), "NOMINA EMPRESA EJEMPLO", 1500.00m, 2476.55m),
                new Transaction(new DateOnly(2025, 1, 10), "COMPRA SUPERMERCADO EJEMPLO", -23.45m, 1000.00m),
            },
            stored);
    }
}
