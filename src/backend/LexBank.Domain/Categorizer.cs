using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LexBank.Domain;

public class Categorizer(IReadOnlyList<Rule> rules)
{
    public Category? Categorize(string description, decimal amount)
    {
        var normalizedDescription = Normalize(description);
        return rules.OrderBy(r => r.Priority)
            .FirstOrDefault(r => r.AcceptsAmount(amount) && Matches(normalizedDescription, Normalize(r.Pattern)))?.Category;
    }

    // A pattern matches whole words; a "*" at either end also accepts a longer word that starts or ends with it.
    private static bool Matches(string text, string pattern)
    {
        var word = pattern.Trim('*');
        var wordStart = pattern.StartsWith('*') ? string.Empty : @"(?<![\p{L}\p{N}])";
        var wordEnd = pattern.EndsWith('*') ? string.Empty : @"(?![\p{L}\p{N}])";
        return Regex.IsMatch(text, wordStart + Regex.Escape(word) + wordEnd);
    }

    private static string Normalize(string text)
    {
        var withoutAccents = text.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(withoutAccents.ToArray()).ToLowerInvariant();
    }
}
