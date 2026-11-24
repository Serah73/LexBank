namespace LexBank.Domain;

public record Rule(string Pattern, Category Category, AmountSign Sign = AmountSign.Any, int Priority = 0)
{
    // A pattern needs a word; at most one "*" is allowed at each end.
    public static bool IsValidPattern(string pattern) =>
        !string.IsNullOrWhiteSpace(pattern)
        && pattern.Trim('*').Length > 0
        && !pattern.StartsWith("**")
        && !pattern.EndsWith("**");

    public bool AcceptsAmount(decimal amount) => Sign switch
    {
        AmountSign.Expense => amount < 0,
        AmountSign.Income => amount > 0,
        _ => true,
    };
}
