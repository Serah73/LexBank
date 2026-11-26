namespace LexBank.Domain.Tests;

public class AccuracyTests
{
    [Fact]
    public void From_NoTransactions_ReturnsZeroOfZero()
    {
        var accuracy = Accuracy.From([]);

        Assert.Equal(new Accuracy(0, 0), accuracy);
    }

    [Fact]
    public void From_RuleCategoryEqualsTransactionCategory_CountsAHit()
    {
        var accuracy = Accuracy.From([Categorized(Category.Groceries, ruleCategory: Category.Groceries)]);

        Assert.Equal(new Accuracy(1, 1), accuracy);
    }

    [Fact]
    public void From_TransactionWithoutRule_CountsAMiss()
    {
        var accuracy = Accuracy.From(
        [
            Categorized(Category.Uncategorized, ruleCategory: null),
            Categorized(Category.Groceries, ruleCategory: null) with { Corrected = true },
            Categorized(Category.Uncategorized, ruleCategory: null) with { Corrected = true },
        ]);

        Assert.Equal(new Accuracy(0, 3), accuracy);
    }

    [Fact]
    public void From_CorrectedToACategoryOtherThanTheRules_CountsAMiss()
    {
        var accuracy = Accuracy.From([Categorized(Category.Restaurants, ruleCategory: Category.Groceries) with { Corrected = true }]);

        Assert.Equal(new Accuracy(0, 1), accuracy);
    }

    [Fact]
    public void From_CorrectedToTheRulesCategory_CountsAHit()
    {
        var accuracy = Accuracy.From([Categorized(Category.Groceries, ruleCategory: Category.Groceries) with { Corrected = true }]);

        Assert.Equal(new Accuracy(1, 1), accuracy);
    }

    [Fact]
    public void From_HitsAndMisses_CountsOnlyTheHits()
    {
        var accuracy = Accuracy.From(
        [
            Categorized(Category.Groceries, ruleCategory: Category.Groceries),
            Categorized(Category.Restaurants, ruleCategory: Category.Groceries) with { Corrected = true },
            Categorized(Category.Uncategorized, ruleCategory: null),
        ]);

        Assert.Equal(new Accuracy(1, 3), accuracy);
    }

    private static Transaction Categorized(Category category, Category? ruleCategory) =>
        new(new DateOnly(2026, 1, 15), "MERCADONA", -20m, 100m, Bank.Ruralvia, category)
        {
            Rule = ruleCategory is null ? null : new Rule("mercadona", ruleCategory.Value),
        };
}
