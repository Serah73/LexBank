using LexBank.Domain;

namespace LexBank.Application;

public class LogIn(IUserRepository repository, IPasswordHasher passwordHasher)
{
    public async Task<User> ExecuteAsync(string email, string password)
    {
        var user = await repository.FindByEmailAsync(email);
        if (user is null || !passwordHasher.Verify(password, user.PasswordHash))
            throw new InvalidCredentialsException();

        return user;
    }
}
