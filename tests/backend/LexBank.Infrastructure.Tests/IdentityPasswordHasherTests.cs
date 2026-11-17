namespace LexBank.Infrastructure.Tests;

public class IdentityPasswordHasherTests
{
    private readonly IdentityPasswordHasher _hasher = new();

    [Fact]
    public void Verify_PasswordThatWasHashed_ReturnsTrue()
    {
        var hash = _hasher.Hash("a-long-password");

        Assert.True(_hasher.Verify("a-long-password", hash));
    }

    [Fact]
    public void Verify_DifferentPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("a-long-password");

        Assert.False(_hasher.Verify("another-password", hash));
    }
}
