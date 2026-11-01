namespace LexBank.Domain;

public record Transaction(DateOnly Date, string Description, decimal Amount, decimal Balance);
