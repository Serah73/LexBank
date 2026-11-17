using LexBank.Application;
using LexBank.Domain;

namespace LexBank.Api.Tests;

public class StubStatementReader : IStatementReader
{
    public IReadOnlyList<Transaction> Transactions { get; set; } = [];

    public bool CanRead(string fileName) => true;

    public StatementReadResult Read(Stream stream) => new(Transactions, []);
}
