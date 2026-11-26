using LexBank.Domain;

namespace LexBank.Application;

public class GetAccuracy(ITransactionRepository repository)
{
    public async Task<Accuracy> ExecuteAsync(string email) => Accuracy.From(await repository.ListAsync(email));
}
