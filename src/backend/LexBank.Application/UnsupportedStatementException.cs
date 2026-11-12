namespace LexBank.Application;

public class UnsupportedStatementException(string message, Exception? innerException = null)
    : Exception(message, innerException);
