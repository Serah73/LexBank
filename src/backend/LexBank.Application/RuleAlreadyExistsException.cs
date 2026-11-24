namespace LexBank.Application;

public class RuleAlreadyExistsException() : Exception("A rule with the same pattern and sign already exists.");
