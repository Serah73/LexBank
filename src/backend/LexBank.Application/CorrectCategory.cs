using LexBank.Domain;

namespace LexBank.Application;

public class CorrectCategory(ITransactionRepository repository)
{
    public async Task<CorrectCategoryResult> ExecuteAsync(string email, int id, Category category)
    {
        var corrected = (await repository.GetAsync(email, id)).Correct(category);
        await repository.UpdateAsync(email, corrected);
        return new(corrected, Accuracy.From(await repository.ListAsync(email)));
    }
}
