namespace LexBank.Domain;

public record Transaction(DateOnly Date, string Description, decimal Amount, decimal Balance, Bank Bank, Category Category = Category.Uncategorized)
{
    public Rule? Rule { get; init; }
}
