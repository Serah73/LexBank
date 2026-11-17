using LexBank.Domain;
using Microsoft.EntityFrameworkCore;

namespace LexBank.Infrastructure.Tests;

public class EfUserRepositoryTests : IDisposable
{
    private readonly LexBankDbContext _context;

    public EfUserRepositoryTests()
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
    public async Task AddAsync_EmailAlreadyStored_Throws()
    {
        var repository = new EfUserRepository(_context);
        await repository.AddAsync(new User("ana@example.com", "hash-1"));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            repository.AddAsync(new User("ana@example.com", "hash-2")));
    }

    [Fact]
    public async Task FindByEmailAsync_EmailStored_ReturnsTheUser()
    {
        var repository = new EfUserRepository(_context);
        await repository.AddAsync(new User("ana@example.com", "hash-1"));

        var user = await repository.FindByEmailAsync("ana@example.com");

        Assert.Equal(new User("ana@example.com", "hash-1"), user);
    }

    [Fact]
    public async Task FindByEmailAsync_EmailNotStored_ReturnsNull()
    {
        var repository = new EfUserRepository(_context);
        await repository.AddAsync(new User("ana@example.com", "hash-1"));

        var user = await repository.FindByEmailAsync("luis@example.com");

        Assert.Null(user);
    }
}
