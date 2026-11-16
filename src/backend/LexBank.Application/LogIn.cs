namespace LexBank.Application;

public class LogIn(IUserRepository repository, IPasswordHasher passwordHasher)
{
    public async Task ExecuteAsync(string email, string password)
    {
        var user = await repository.FindByEmailAsync(email);
        if (user is null || !passwordHasher.Verify(password, user.PasswordHash))
            throw new InvalidCredentialsException();
    }
}
