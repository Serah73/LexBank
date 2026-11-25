using LexBank.Domain;

namespace LexBank.Application;

public class ApplyRules(ITransactionRepository repository, IRuleRepository ruleRepository)
{
    public async Task<int> ExecuteAsync(string email)
    {
        var categorizer = new Categorizer(await ruleRepository.ListAsync());
        var changed = 0;
        foreach (var transaction in await repository.ListAsync(email))
        {
            var categorization = categorizer.Categorize(transaction.Description, transaction.Amount);
            // The user's correction is kept; the rule always holds what the engine decides now.
            var category = transaction.Corrected ? transaction.Category : categorization.Category;
            if (category == transaction.Category && categorization.Rule?.Id == transaction.Rule?.Id) continue;

            await repository.UpdateAsync(email, transaction with { Category = category, Rule = categorization.Rule });
            if (category != transaction.Category) changed++;
        }

        return changed;
    }
}
