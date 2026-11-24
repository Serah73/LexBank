namespace LexBank.Application;

public class InvalidRulePatternException() : Exception("The pattern of a rule needs a word and at most one \"*\" at each end.");
