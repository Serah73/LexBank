namespace LexBank.Domain;

public record Accuracy(int Hits, int Total)
{
    public static Accuracy From(IEnumerable<Transaction> transactions)
    {
        var list = transactions.ToList();
        return new(list.Count(t => t.Rule?.Category == t.Category), list.Count);
    }
}
