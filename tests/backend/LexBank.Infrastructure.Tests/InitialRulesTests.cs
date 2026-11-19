namespace LexBank.Infrastructure.Tests;

public class InitialRulesTests
{
    [Fact]
    public void All_EveryRule_HasAUniqueId()
    {
        var ids = InitialRules.All.Select(r => r.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Theory]
    [InlineData("interés", 101)]
    [InlineData("*mercadona*", 615)]
    [InlineData("bocata", 709)]
    [InlineData("devolución", 1804)]
    public void All_ARule_HasItsPriorityTimesOneHundredPlusItsPositionAsId(string pattern, int expectedId)
    {
        var id = InitialRules.All.Single(r => r.Rule.Pattern == pattern).Id;

        Assert.Equal(expectedId, id);
    }
}
