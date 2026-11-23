namespace LexBank.Domain.Tests;

public class TransactionTests
{
    [Fact]
    public void Correct_AnotherCategory_ChangesTheCategoryMarksItAsCorrectedAndKeepsTheRule()
    {
        var rule = new Rule("*mercadona*", Category.Groceries, AmountSign.Expense, 1);
        var transaction = new Transaction(new DateOnly(2025, 1, 10), "tj-mercadona", -23.40m, 100m, Bank.Ruralvia, Category.Groceries)
        {
            Rule = rule,
        };

        var corrected = transaction.Correct(Category.Restaurants);

        Assert.Equal(Category.Restaurants, corrected.Category);
        Assert.True(corrected.Corrected);
        Assert.Equal(rule, corrected.Rule);
        Assert.False(transaction.Corrected);
    }

    [Fact]
    public void Correct_TheCurrentCategory_ReturnsTheTransactionUnchanged()
    {
        var rule = new Rule("*mercadona*", Category.Groceries, AmountSign.Expense, 1);
        var transaction = new Transaction(new DateOnly(2025, 1, 10), "tj-mercadona", -23.40m, 100m, Bank.Ruralvia, Category.Groceries)
        {
            Rule = rule,
        };

        var corrected = transaction.Correct(Category.Groceries);

        Assert.Equal(transaction, corrected);
        Assert.False(corrected.Corrected);
    }
}
