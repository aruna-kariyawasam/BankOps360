namespace BankOps.Web.Services;

public interface IClearingBankClient
{
    Task<ClearingPaymentResult> SubmitAsync(
        ClearingPaymentRequest request,
        CancellationToken cancellationToken = default);
}