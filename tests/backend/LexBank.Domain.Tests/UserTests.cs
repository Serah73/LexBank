namespace LexBank.Domain.Tests;

public class UserTests
{
    [Fact]
    public void ToString_WithAPasswordHash_DoesNotRevealTheHash()
    {
        var user = new User("ana@example.com", "secret-hash-value");

        var text = user.ToString();

        Assert.DoesNotContain("secret-hash-value", text);
    }
}
