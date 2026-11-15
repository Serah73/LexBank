using System.Text;

namespace LexBank.Domain;

public record User(string Email, string PasswordHash)
{
    protected virtual bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"Email = {Email}");
        return true;
    }
}
