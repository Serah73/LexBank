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
        var storedCopies = (await repository.ListAsync(email))
            .GroupBy(Key)
            .ToDictionary(g => g.Key, g => new Queue<Transaction>(g));

        var newTransactions = new List<Transaction>();
        var skipped = 0;
        var returned = new List<Transaction?>();
        foreach (var transaction in transactions)
        {
            if (storedCopies.TryGetValue(Key(transaction), out var copies) && copies.Count > 0)
            {
                returned.Add(copies.Dequeue());
                skipped++;
            }
            else
            {
                returned.Add(null);
                newTransactions.Add(transaction);
            }
        }

        var added = new Queue<Transaction>(await repository.AddAsync(email, newTransactions));
        return new UploadStatementResult(
            returned.Select(t => t ?? added.Dequeue()).ToList(),
            result.FailedRows,
            skipped,
            Accuracy.From(await repository.ListAsync(email)));
    }

    // What identifies a row of a statement: the category and the rule are not part of it, because the user can change them.
    private static (DateOnly, string, decimal, decimal, Bank) Key(Transaction t) =>
        (t.Date, t.Description, t.Amount, t.Balance, t.Bank);
}
