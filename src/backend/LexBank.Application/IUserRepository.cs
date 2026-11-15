using LexBank.Domain;

namespace LexBank.Application;

public interface IUserRepository
{
    Task AddAsync(User user);

    Task<bool> ExistsAsync(string email);
}
