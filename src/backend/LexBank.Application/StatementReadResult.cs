using LexBank.Domain;

namespace LexBank.Application;

public record StatementReadResult(IReadOnlyList<Transaction> Transactions);
