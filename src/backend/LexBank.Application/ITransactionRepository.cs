using LexBank.Domain;

namespace LexBank.Application;

public interface ITransactionRepository
{
    Task AddAsync(IEnumerable<Transaction> transactions);

    Task<IReadOnlyList<Transaction>> ListAsync();
}
