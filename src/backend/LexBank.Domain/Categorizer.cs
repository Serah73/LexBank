using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LexBank.Domain;

public class Categorizer(IReadOnlyList<Rule> rules)
{
    public Category? Categorize(string description, decimal amount)
    {
        var normalizedDescription = Normalize(description);
        return rules.OrderBy(r => r.Priority).FirstOrDefault(r => r.AcceptsAmount(amount) && ContainsWord(normalizedDescription, Normalize(r.Pattern)))?.Category;
    }

    private static bool ContainsWord(string text, string word) =>
        Regex.IsMatch(text, $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(word)}(?![\p{{L}}\p{{N}}])");

    private static string Normalize(string text)
    {
        var withoutAccents = text.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(withoutAccents.ToArray()).ToLowerInvariant();
    }
}
