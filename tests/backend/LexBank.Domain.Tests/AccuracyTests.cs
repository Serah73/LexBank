namespace LexBank.Domain.Tests;

public class AccuracyTests
{
    [Fact]
    public void From_NoTransactions_ReturnsZeroOfZero()
    {
        var accuracy = Accuracy.From([]);

        AssertTotals(0, 0, accuracy);
    }

    [Fact]
    public void From_RuleCategoryEqualsTransactionCategory_CountsAHit()
    {
        var accuracy = Accuracy.From([Categorized(Category.Groceries, ruleCategory: Category.Groceries)]);

        AssertTotals(1, 1, accuracy);
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

        AssertTotals(0, 3, accuracy);
    }

    [Fact]
    public void From_CorrectedToACategoryOtherThanTheRules_CountsAMiss()
    {
        var accuracy = Accuracy.From([Categorized(Category.Restaurants, ruleCategory: Category.Groceries) with { Corrected = true }]);

        AssertTotals(0, 1, accuracy);
    }

    [Fact]
    public void From_CorrectedToTheRulesCategory_CountsAHit()
    {
        var accuracy = Accuracy.From([Categorized(Category.Groceries, ruleCategory: Category.Groceries) with { Corrected = true }]);

        AssertTotals(1, 1, accuracy);
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

        AssertTotals(1, 3, accuracy);
    }

    [Fact]
    public void From_TransactionsOfSeveralBanks_GroupsHitsAndTotalByBankInBankOrder()
    {
        var accuracy = Accuracy.From(
        [
            Categorized(Category.Groceries, ruleCategory: Category.Groceries) with { Bank = Bank.Revolut },
            Categorized(Category.Groceries, ruleCategory: Category.Groceries),
            Categorized(Category.Uncategorized, ruleCategory: null),
            Categorized(Category.Uncategorized, ruleCategory: null) with { Bank = Bank.Revolut },
            Categorized(Category.Uncategorized, ruleCategory: null) with { Bank = Bank.Revolut },
        ]);

        Assert.Equal([new BankAccuracy(Bank.Ruralvia, 1, 2), new BankAccuracy(Bank.Revolut, 1, 3)], accuracy.ByBank);
    }

    [Fact]
    public void From_TransactionsOfSeveralCategories_GroupsHitsAndTotalByFinalCategoryInCategoryOrder()
    {
        var accuracy = Accuracy.From(
        [
            Categorized(Category.Uncategorized, ruleCategory: null),
            Categorized(Category.Restaurants, ruleCategory: Category.Restaurants),
            Categorized(Category.Groceries, ruleCategory: Category.Groceries),
            Categorized(Category.Restaurants, ruleCategory: null),
        ]);

        Assert.Equal(
            [
                new CategoryAccuracy(Category.Groceries, 1, 1),
                new CategoryAccuracy(Category.Restaurants, 1, 2),
                new CategoryAccuracy(Category.Uncategorized, 0, 1),
            ],
            accuracy.ByCategory);
    }

    [Fact]
    public void From_CorrectedTransaction_CountsInItsNewCategory()
    {
        var groceries = Categorized(Category.Groceries, ruleCategory: Category.Groceries);

        var accuracy = Accuracy.From([groceries.Correct(Category.Restaurants)]);

        Assert.Equal([new CategoryAccuracy(Category.Restaurants, 0, 1)], accuracy.ByCategory);
    }

    [Fact]
    public void From_NoTransactions_ReturnsNoGroups()
    {
        var accuracy = Accuracy.From([]);

        Assert.Empty(accuracy.ByBank);
        Assert.Empty(accuracy.ByCategory);
    }

    private static void AssertTotals(int hits, int total, Accuracy accuracy)
    {
        Assert.Equal(hits, accuracy.Hits);
        Assert.Equal(total, accuracy.Total);
    }

    private static Transaction Categorized(Category category, Category? ruleCategory) =>
        new(new DateOnly(2026, 1, 15), "MERCADONA", -20m, 100m, Bank.Ruralvia, category)
        {
            Rule = ruleCategory is null ? null : new Rule("mercadona", ruleCategory.Value),
        };
}
