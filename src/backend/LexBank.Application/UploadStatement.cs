using LexBank.Domain;

namespace LexBank.Application;

public class UploadStatement(IEnumerable<IStatementReader> readers, ITransactionRepository repository)
{
    public async Task<UploadStatementResult> ExecuteAsync(string email, string fileName, Stream stream)
    {
        var reader = readers.FirstOrDefault(r => r.CanRead(fileName))
            ?? throw new UnsupportedStatementException("The file format is not supported");
        var result = reader.Read(stream);
        var storedCopies = (await repository.ListAsync(email)).GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());

        var newTransactions = new List<Transaction>();
        foreach (var transaction in result.Transactions)
        {
            if (storedCopies.GetValueOrDefault(transaction) > 0)
                storedCopies[transaction]--;
            else
                newTransactions.Add(transaction);
        }

        await repository.AddAsync(email, newTransactions);
        return new UploadStatementResult(
            result.Transactions,
            result.FailedRows,
            result.Transactions.Count - newTransactions.Count);
    }
}
