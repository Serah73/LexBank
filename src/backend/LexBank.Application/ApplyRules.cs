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

            // A transaction corrected while the rules are applied is left as the user set it.
            var updated = await repository.TryUpdateCategorizationAsync(email, transaction, category, categorization.Rule);
            if (updated && category != transaction.Category) changed++;
        }

        return changed;
    }
}
