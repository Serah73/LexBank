using LexBank.Domain;

namespace LexBank.Application;

public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> AddAsync(string email, IEnumerable<Transaction> transactions);

    Task<IReadOnlyList<Transaction>> ListAsync(string email);

    /// <exception cref="TransactionNotFoundException">There is no transaction with that id among the user's.</exception>
    Task<Transaction> GetAsync(string email, int id);

    /// <exception cref="TransactionNotFoundException">There is no transaction with that id among the user's.</exception>
    Task UpdateAsync(string email, Transaction transaction);
}
