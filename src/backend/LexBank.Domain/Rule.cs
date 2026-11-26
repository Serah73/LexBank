namespace LexBank.Domain;

public record Rule(string Pattern, Category Category, AmountSign Sign = AmountSign.Any, int Priority = 0)
{
    public const int MaxPatternLength = 100;

    public int Id { get; init; }

    // A pattern needs a letter or a digit, has a maximum length and allows at most one "*" at each end.
    public static bool IsValidPattern(string pattern) =>
        !string.IsNullOrWhiteSpace(pattern)
        && pattern.Length <= MaxPatternLength
        && pattern.Any(char.IsLetterOrDigit)
        && !pattern.StartsWith("**")
        && !pattern.EndsWith("**");

    public bool AcceptsAmount(decimal amount) => Sign switch
    {
        AmountSign.Expense => amount < 0,
        AmountSign.Income => amount > 0,
        _ => true,
    };
}
