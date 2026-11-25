using LexBank.Application;
using LexBank.Domain;
using Microsoft.EntityFrameworkCore;

namespace LexBank.Infrastructure;

public class EfRuleRepository(LexBankDbContext context) : IRuleRepository
{
    public async Task<IReadOnlyList<Rule>> ListAsync() =>
        await context.Set<Rule>().AsNoTracking().OrderBy(r => r.Priority).ThenBy(r => r.Id).ToListAsync();

    public async Task<Rule> GetAsync(int id) =>
        await context.Set<Rule>().AsNoTracking().SingleOrDefaultAsync(r => r.Id == id) ?? throw new RuleNotFoundException("The rule does not exist");

    public async Task AddAsync(Rule rule)
    {
        context.Set<Rule>().Add(rule);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Rule rule)
    {
        context.Set<Rule>().Update(rule);
        await context.SaveChangesAsync();
    }
}
