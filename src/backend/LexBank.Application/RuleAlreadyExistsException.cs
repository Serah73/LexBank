using LexBank.Domain;

namespace LexBank.Application;

public class RuleAlreadyExistsException(Rule existing) : Exception("A rule with the same pattern and sign already exists.")
{
    public Rule Existing { get; } = existing;
}
