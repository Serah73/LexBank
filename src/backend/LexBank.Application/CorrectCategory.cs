using LexBank.Domain;

namespace LexBank.Application;

public class CorrectCategory(ITransactionRepository repository)
{
    public async Task<Transaction> ExecuteAsync(string email, int id, Category category)
    {
        var corrected = (await repository.GetAsync(email, id)).Correct(category);
        await repository.UpdateAsync(email, corrected);
        return corrected;
    }
}
