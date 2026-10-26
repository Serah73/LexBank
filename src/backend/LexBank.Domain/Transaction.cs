namespace LexBank.Domain;

public record Transaction(DateOnly Date, string Description, decimal Amount, string Currency, decimal Balance);
