namespace LexBank.Application;

public class UploadStatement(IStatementReader reader, ITransactionRepository repository)
{
    public async Task<StatementReadResult> ExecuteAsync(Stream stream)
    {
        var result = reader.Read(stream);
        await repository.AddAsync(result.Transactions);
        return result;
    }
}
