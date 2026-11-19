namespace LexBank.Domain.Tests;

public class CategorizerTests
{
    [Fact]
    public void Categorize_DescriptionContainsPatternWord_ReturnsRuleCategory()
    {
        var categorizer = new Categorizer([new Rule("mercadona", Category.Groceries)]);

        var category = categorizer.Categorize("tj-mercadona finca espana", -23.40m);

        Assert.Equal(Category.Groceries, category);
    }

    [Fact]
    public void Categorize_PatternInsideAnotherWord_DoesNotMatch()
    {
        var categorizer = new Categorizer([new Rule("bar", Category.Groceries)]);

        var category = categorizer.Categorize("tj-barcelona airport", -10m);

        Assert.Null(category);
    }

    [Fact]
    public void Categorize_DifferentCaseAndAccents_Matches()
    {
        var categorizer = new Categorizer([new Rule("cafetería", Category.Groceries)]);

        var category = categorizer.Categorize("TJ-CAFETERIA SAN FRANCISCO", -3.5m);

        Assert.Equal(Category.Groceries, category);
    }

    [Fact]
    public void Categorize_MultiWordPattern_Matches()
    {
        var categorizer = new Categorizer([new Rule("cuenta remunerada", Category.Groceries)]);

        var category = categorizer.Categorize("A EUR Cuenta Remunerada", -910m);

        Assert.Equal(Category.Groceries, category);
    }

    [Theory]
    [InlineData(AmountSign.Expense, -10, true)]
    [InlineData(AmountSign.Expense, 10, false)]
    [InlineData(AmountSign.Income, 10, true)]
    [InlineData(AmountSign.Income, -10, false)]
    [InlineData(AmountSign.Any, -10, true)]
    [InlineData(AmountSign.Any, 10, true)]
    public void Categorize_AmountSignAndRuleSign_MatchesOnlyWhenCompatible(AmountSign sign, int amount, bool matches)
    {
        var categorizer = new Categorizer([new Rule("bizum", Category.Groceries, sign)]);

        var category = categorizer.Categorize("cargo bizum - cena", amount);

        Assert.Equal(matches ? Category.Groceries : null, category);
    }

    [Fact]
    public void Categorize_SeveralRulesMatch_ReturnsHighestPriority()
    {
        var categorizer = new Categorizer([
            new Rule("bizum", Category.Groceries, Priority: 2),
            new Rule("bocata", Category.Restaurants, Priority: 1),
        ]);

        var category = categorizer.Categorize("cargo bizum - bocata", -5m);

        Assert.Equal(Category.Restaurants, category);
    }

    [Fact]
    public void Categorize_NoRuleMatches_ReturnsNull()
    {
        var categorizer = new Categorizer([new Rule("mercadona", Category.Groceries)]);

        var category = categorizer.Categorize("tj-zara tenerife", -13.05m);

        Assert.Null(category);
    }

    [Fact]
    public void Categorize_PatternWithTrailingWildcard_MatchesWordsStartingWithIt()
    {
        var categorizer = new Categorizer([new Rule("binter*", Category.Groceries)]);

        var category = categorizer.Categorize("tj-bintercanarias ejemplo", -45m);

        Assert.Equal(Category.Groceries, category);
    }

    [Fact]
    public void Categorize_PatternWithLeadingWildcard_MatchesWordsEndingWithIt()
    {
        var categorizer = new Categorizer([new Rule("*canarias", Category.Groceries)]);

        var category = categorizer.Categorize("tj-bintercanarias ejemplo", -45m);

        Assert.Equal(Category.Groceries, category);
    }

    [Fact]
    public void Categorize_PatternWithWildcardAtBothEnds_MatchesInsideAWord()
    {
        var categorizer = new Categorizer([new Rule("*binter*", Category.Groceries)]);

        var category = categorizer.Categorize("tj-xbintercanariasx", -45m);

        Assert.Equal(Category.Groceries, category);
    }
}
