namespace BankOps.Web.Services;

public sealed class BankingOperationException : Exception
{
    public int StatusCode { get; }

    public BankingOperationException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }
}