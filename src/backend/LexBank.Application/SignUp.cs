using LexBank.Domain;

namespace LexBank.Application;

public class SignUp(IUserRepository repository, IPasswordHasher passwordHasher)
{
    public async Task ExecuteAsync(string email, string password)
    {
        if (await repository.ExistsAsync(email))
            throw new EmailAlreadyRegisteredException();

        await repository.AddAsync(new User(email, passwordHasher.Hash(password)));
    }
}
