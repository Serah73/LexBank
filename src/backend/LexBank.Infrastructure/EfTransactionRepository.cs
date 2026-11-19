using LexBank.Application;
using LexBank.Domain;
using Microsoft.EntityFrameworkCore;

namespace LexBank.Infrastructure;

public class EfTransactionRepository(LexBankDbContext context) : ITransactionRepository
{
    public async Task AddAsync(string email, IEnumerable<Transaction> transactions)
    {
        var userId = await UserIdAsync(email);
        var storedRules = await context.Set<Rule>().ToListAsync();
        foreach (var transaction in transactions)
        {
            // Point to the stored rule, so saving does not insert a copy of it.
            var toStore = transaction with { Rule = transaction.Rule is null ? null : storedRules.Single(r => r == transaction.Rule) };
            context.Set<Transaction>().Add(toStore);
            context.Entry(toStore).Property("UserId").CurrentValue = userId;
        }

        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Transaction>> ListAsync(string email)
    {
        var userId = await UserIdAsync(email);
        return await context.Set<Transaction>()
            .AsNoTracking()
            .Include(t => t.Rule)
            .Where(t => EF.Property<int>(t, "UserId") == userId)
            .OrderByDescending(t => t.Date)
            .ThenBy(t => EF.Property<int>(t, "Id"))
            .ToListAsync();
    }

    private async Task<int> UserIdAsync(string email) =>
        await context.Set<User>().Where(u => u.Email == email).Select(u => EF.Property<int>(u, "Id")).SingleAsync();
}
