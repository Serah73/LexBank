using LexBank.Domain;

namespace LexBank.Application;

public class CreateRule(IRuleRepository repository)
{
    // A priority has four digits: the first is its type (1 = created by a user) and the rest its number inside the type.
    private const int FirstUserPriority = 1999;
    private const int LowestUserPriority = 1000;

    public async Task<Rule> ExecuteAsync(string pattern, Category category, AmountSign sign)
    {
        pattern = pattern.Trim();
        if (!Rule.IsValidPattern(pattern)) throw new InvalidRulePatternException();

        var rules = await repository.ListAsync();
        var existing = rules.FirstOrDefault(r => r.Sign == sign && string.Equals(r.Pattern, pattern, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) throw new RuleAlreadyExistsException(existing);

        // The newest rule has the lowest number, so it wins over the older ones.
        var userPriorities = rules.Select(r => r.Priority).Where(p => p is >= LowestUserPriority and <= FirstUserPriority).ToList();
        var rule = new Rule(pattern, category, sign, userPriorities.Count == 0 ? FirstUserPriority : userPriorities.Min() - 1);
        await repository.AddAsync(rule);
        return rule;
    }
}
