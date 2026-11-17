using LexBank.Domain;

namespace LexBank.Application;

public interface ITransactionRepository
{
    Task AddAsync(string email, IEnumerable<Transaction> transactions);

    Task<IReadOnlyList<Transaction>> ListAsync(string email);
}
