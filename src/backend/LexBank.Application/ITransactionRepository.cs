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

    /// <summary>
    /// Stores the category and the rule decided by the engine, only if the transaction of the user has not been
    /// corrected or recategorized since it was read. It never writes the correction mark.
    /// </summary>
    /// <returns>Whether the transaction was updated.</returns>
    Task<bool> TryUpdateCategorizationAsync(string email, Transaction asRead, Category category, Rule? rule);
}
