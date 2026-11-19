using LexBank.Domain;

namespace LexBank.Application;

public class UploadStatement(
    IEnumerable<IStatementReader> readers,
    ITransactionRepository repository,
    IRuleRepository ruleRepository)
{
    public async Task<UploadStatementResult> ExecuteAsync(string email, string fileName, Stream stream)
    {
        var reader = readers.FirstOrDefault(r => r.CanRead(fileName))
            ?? throw new UnsupportedStatementException("The file format is not supported");
        var result = reader.Read(stream);
        var categorizer = new Categorizer(await ruleRepository.ListAsync());
        var transactions = result.Transactions
            .Select(t =>
            {
                var categorization = categorizer.Categorize(t.Description, t.Amount);
                return t with { Category = categorization.Category, Rule = categorization.Rule };
            })
            .ToList();
        var storedCopies = (await repository.ListAsync(email)).GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());

        var newTransactions = new List<Transaction>();
        foreach (var transaction in transactions)
        {
            if (storedCopies.GetValueOrDefault(transaction) > 0)
                storedCopies[transaction]--;
            else
                newTransactions.Add(transaction);
        }

        await repository.AddAsync(email, newTransactions);
        return new UploadStatementResult(
            transactions,
            result.FailedRows,
            transactions.Count - newTransactions.Count);
    }
}
