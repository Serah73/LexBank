using LexBank.Application;
using LexBank.Domain;
using Microsoft.EntityFrameworkCore;

namespace LexBank.Infrastructure;

public class EfUserRepository(LexBankDbContext context) : IUserRepository
{
    public async Task AddAsync(User user)
    {
        context.Set<User>().Add(user);
        await context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(string email) =>
        await context.Set<User>().AnyAsync(u => u.Email == email);
}
