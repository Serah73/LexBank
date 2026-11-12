using LexBank.Application;
using LexBank.Domain;
using Microsoft.EntityFrameworkCore;

namespace LexBank.Infrastructure;

public class EfTransactionRepository(LexBankDbContext context) : ITransactionRepository
{
    public async Task AddAsync(IEnumerable<Transaction> transactions)
    {
        context.Set<Transaction>().AddRange(transactions);
        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Transaction>> ListAsync() =>
        await context.Set<Transaction>().AsNoTracking().OrderByDescending(t => t.Date).ThenBy(t => EF.Property<int>(t, "Id")).ToListAsync();
}
