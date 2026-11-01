using BankOps.Web.Models.Banking.DTOs;

namespace BankOps.Web.Services;

public interface IPaymentProcessingService
{
    Task<PaymentResponse> ProcessPaymentAsync(
        CreatePaymentRequest request,
        string? userId,
        CancellationToken cancellationToken = default);

    Task<PaymentResponse?> GetPaymentAsync(
        string paymentReference,
        CancellationToken cancellationToken = default);
}