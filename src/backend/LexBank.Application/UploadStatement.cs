using LexBank.Domain;

namespace LexBank.Application;

public class UploadStatement(IStatementReader reader, ITransactionRepository repository)
{
    public async Task<UploadStatementResult> ExecuteAsync(Stream stream)
    {
        var result = reader.Read(stream);
        var storedCopies = (await repository.ListAsync()).GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());

        var newTransactions = new List<Transaction>();
        foreach (var transaction in result.Transactions)
        {
            if (storedCopies.GetValueOrDefault(transaction) > 0)
                storedCopies[transaction]--;
            else
                newTransactions.Add(transaction);
        }

        await repository.AddAsync(newTransactions);
        return new UploadStatementResult(
            result.Transactions,
            result.FailedRows,
            result.Transactions.Count - newTransactions.Count);
    }
}
