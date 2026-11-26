namespace LexBank.Domain.Tests;

public class RuleTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("*")]
    [InlineData("**")]
    [InlineData("**foo")]
    [InlineData("foo**")]
    [InlineData("* *")]
    [InlineData("*-*")]
    [InlineData(".")]
    public void IsValidPattern_NoLetterOrDigitOrRepeatedStars_ReturnsFalse(string pattern)
    {
        Assert.False(Rule.IsValidPattern(pattern));
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("*foo")]
    [InlineData("foo*")]
    [InlineData("*foo*")]
    [InlineData("cuenta remunerada")]
    [InlineData("*24*")]
    public void IsValidPattern_WordsWithOrWithoutOneStarAtEachEnd_ReturnsTrue(string pattern)
    {
        Assert.True(Rule.IsValidPattern(pattern));
    }
}
