using LexBank.Application;
using LexBank.Domain;
using Microsoft.EntityFrameworkCore;

namespace LexBank.Infrastructure;

public class EfTransactionRepository(LexBankDbContext context) : ITransactionRepository
{
    public async Task<IReadOnlyList<Transaction>> AddAsync(string email, IEnumerable<Transaction> transactions)
    {
        var userId = await UserIdAsync(email);
        var storedRules = await context.Set<Rule>().ToListAsync();
        var stored = new List<Transaction>();
        foreach (var transaction in transactions)
        {
            // Point to the stored rule, so saving does not insert a copy of it.
            var toStore = transaction with { Rule = transaction.Rule is null ? null : storedRules.Single(r => r.Id == transaction.Rule.Id) };
            context.Set<Transaction>().Add(toStore);
            context.Entry(toStore).Property("UserId").CurrentValue = userId;
            stored.Add(toStore);
        }

        await context.SaveChangesAsync();
        return stored;
    }

    public async Task<IReadOnlyList<Transaction>> ListAsync(string email)
    {
        var userId = await UserIdAsync(email);
        return await OfUser(userId)
            .AsNoTracking()
            .Include(t => t.Rule)
            .OrderByDescending(t => t.Date)
            .ThenBy(t => t.Id)
            .ToListAsync();
    }

    public async Task<Transaction> GetAsync(string email, int id)
    {
        var userId = await UserIdAsync(email);
        return await OfUser(userId)
            .AsNoTracking()
            .Include(t => t.Rule)
            .Where(t => t.Id == id)
            .SingleOrDefaultAsync() ?? throw new TransactionNotFoundException("The transaction does not exist");
    }

    public async Task UpdateAsync(string email, Transaction transaction)
    {
        var userId = await UserIdAsync(email);
        var updated = await OfUser(userId)
            .Where(t => t.Id == transaction.Id)
            .ExecuteUpdateAsync(set => set
                .SetProperty(t => t.Category, transaction.Category)
                .SetProperty(t => t.Corrected, transaction.Corrected));
        if (updated == 0) throw new TransactionNotFoundException("The transaction does not exist");
    }

    public async Task<bool> TryUpdateCategorizationAsync(string email, Transaction asRead, Category category, Rule? rule)
    {
        var userId = await UserIdAsync(email);
        var ruleId = rule?.Id;
        var updated = await OfUser(userId)
            .Where(t => t.Id == asRead.Id && t.Corrected == asRead.Corrected && t.Category == asRead.Category)
            .ExecuteUpdateAsync(set => set
                .SetProperty(t => EF.Property<int?>(t, "RuleId"), ruleId)
                .SetProperty(t => t.Category, category));
        return updated == 1;
    }

    // Every query on stored transactions starts here: a user only reaches their own.
    private IQueryable<Transaction> OfUser(int userId) =>
        context.Set<Transaction>().Where(t => EF.Property<int>(t, "UserId") == userId);

    private async Task<int> UserIdAsync(string email) =>
        await context.Set<User>().Where(u => u.Email == email).Select(u => EF.Property<int>(u, "Id")).SingleAsync();
}
