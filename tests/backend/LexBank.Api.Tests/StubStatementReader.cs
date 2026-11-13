using LexBank.Application;
using LexBank.Domain;

namespace LexBank.Api.Tests;

public class StubStatementReader : IStatementReader
{
    public IReadOnlyList<Transaction> Transactions { get; set; } = [];

    public StatementReadResult Read(Stream stream) => new(Transactions, []);
}
