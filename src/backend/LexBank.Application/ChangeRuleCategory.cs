using LexBank.Domain;

namespace LexBank.Application;

public class ChangeRuleCategory(IRuleRepository repository)
{
    public async Task<Rule> ExecuteAsync(int id, Category category)
    {
        var changed = (await repository.GetAsync(id)) with { Category = category };
        await repository.UpdateAsync(changed);
        return changed;
    }
}
