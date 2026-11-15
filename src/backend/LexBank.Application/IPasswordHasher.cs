namespace LexBank.Application;

public interface IPasswordHasher
{
    string Hash(string password);
}
