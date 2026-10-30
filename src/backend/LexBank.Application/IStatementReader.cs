namespace LexBank.Application;

public interface IStatementReader
{
    StatementReadResult Read(Stream stream);
}
