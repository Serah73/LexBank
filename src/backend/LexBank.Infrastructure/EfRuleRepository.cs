using LexBank.Application;
using LexBank.Domain;
using Microsoft.EntityFrameworkCore;

namespace LexBank.Infrastructure;

public class EfRuleRepository(LexBankDbContext context) : IRuleRepository
{
    public async Task<IReadOnlyList<Rule>> ListAsync() =>
        await context.Set<Rule>().AsNoTracking().ToListAsync();

    public async Task AddAsync(Rule rule)
    {
        context.Set<Rule>().Add(rule);
        await context.SaveChangesAsync();
    }
}
