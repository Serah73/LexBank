using LexBank.Domain;

namespace LexBank.Application;

public interface IRuleRepository
{
    Task<IReadOnlyList<Rule>> ListAsync();

    Task AddAsync(Rule rule);
}
