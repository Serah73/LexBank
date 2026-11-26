namespace LexBank.Domain;

public record Accuracy(int Hits, int Total, IReadOnlyList<BankAccuracy> ByBank, IReadOnlyList<CategoryAccuracy> ByCategory)
{
    public static Accuracy From(IEnumerable<Transaction> transactions)
    {
        var list = transactions.ToList();
        var byBank = list.GroupBy(t => t.Bank).OrderBy(g => g.Key)
            .Select(g => new BankAccuracy(g.Key, g.Count(IsHit), g.Count()));
        var byCategory = list.GroupBy(t => t.Category).OrderBy(g => g.Key)
            .Select(g => new CategoryAccuracy(g.Key, g.Count(IsHit), g.Count()));
        return new(list.Count(IsHit), list.Count, byBank.ToList(), byCategory.ToList());
    }

    private static bool IsHit(Transaction transaction) => transaction.Rule?.Category == transaction.Category;
}
