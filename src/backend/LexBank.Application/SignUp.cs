using LexBank.Domain;

namespace LexBank.Application;

public class SignUp(IUserRepository repository, IPasswordHasher passwordHasher)
{
    private const int MinimumPasswordLength = 8;

    public async Task ExecuteAsync(string email, string password)
    {
        if (!HasTextOnBothSidesOfTheAtSign(email))
            throw new InvalidEmailException();

        if (password.Length < MinimumPasswordLength)
            throw new InvalidPasswordException();

        if (await repository.ExistsAsync(email))
            throw new EmailAlreadyRegisteredException();

        await repository.AddAsync(new User(email, passwordHasher.Hash(password)));
    }

    private static bool HasTextOnBothSidesOfTheAtSign(string email)
    {
        var atSign = email.IndexOf('@');
        return atSign > 0 && atSign < email.Length - 1;
    }
}
