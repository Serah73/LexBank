namespace LexBank.Domain;

public record Transaction(DateOnly Date, string Description, decimal Amount, decimal Balance, Bank Bank, Category Category = Category.Uncategorized)
{
    public int Id { get; init; }

    public Rule? Rule { get; init; }

    public bool Corrected { get; init; }

    public Transaction Correct(Category category) => category == Category ? this : this with { Category = category, Corrected = true };
}
