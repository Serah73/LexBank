namespace LexBank.Application;

public interface IStatementReader
{
    bool CanRead(string fileName);

    StatementReadResult Read(Stream stream);
}
