namespace LexBank.Domain;

public record Rule(string Pattern, Category Category, AmountSign Sign = AmountSign.Any, int Priority = 0)
{
    public bool AcceptsAmount(decimal amount) => Sign switch
    {
        AmountSign.Expense => amount < 0,
        AmountSign.Income => amount > 0,
        _ => true,
    };
}
