using LexBank.Application;
using LexBank.Domain;
using Microsoft.EntityFrameworkCore;

namespace LexBank.Infrastructure;

public class EfTransactionRepository(LexBankDbContext context) : ITransactionRepository
{
    public async Task AddAsync(string email, IEnumerable<Transaction> transactions)
    {
        var userId = await UserIdAsync(email);
        foreach (var transaction in transactions)
        {
            context.Set<Transaction>().Add(transaction);
            context.Entry(transaction).Property("UserId").CurrentValue = userId;
        }

        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Transaction>> ListAsync(string email)
    {
        var userId = await UserIdAsync(email);
        return await context.Set<Transaction>()
            .AsNoTracking()
            .Where(t => EF.Property<int>(t, "UserId") == userId)
            .OrderByDescending(t => t.Date)
            .ThenBy(t => EF.Property<int>(t, "Id"))
            .ToListAsync();
    }

    private async Task<int> UserIdAsync(string email) =>
        await context.Set<User>().Where(u => u.Email == email).Select(u => EF.Property<int>(u, "Id")).SingleAsync();
}
