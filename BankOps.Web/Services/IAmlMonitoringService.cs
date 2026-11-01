using BankOps.Web.Models.Banking;
using BankOps.Web.Models.Banking.DTOs;

namespace BankOps.Web.Services;

public interface IAmlMonitoringService
{
    Task<IReadOnlyList<AmlAlertResponse>> EvaluateTransactionAsync(
        long transactionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AmlAlertResponse>> GetAlertsAsync(
        AmlAlertStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AmlAlertResponse?> GetAlertAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<AmlAlertResponse> ReviewAlertAsync(
        long id,
        ReviewAmlAlertRequest request,
        string? userId,
        CancellationToken cancellationToken = default);
}