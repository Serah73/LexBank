using LexBank.Domain;

namespace LexBank.Application;

public interface IRuleRepository
{
    Task<IReadOnlyList<Rule>> ListAsync();

    Task<Rule> GetAsync(int id);

    Task AddAsync(Rule rule);

    Task UpdateAsync(Rule rule);
}
