using LexBank.Application;
using LexBank.Domain;
using Microsoft.AspNetCore.Identity;

namespace LexBank.Infrastructure;

public class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(new User(string.Empty, string.Empty), password);

    public bool Verify(string password, string hash) =>
        _hasher.VerifyHashedPassword(new User(string.Empty, string.Empty), hash, password) != PasswordVerificationResult.Failed;
}
