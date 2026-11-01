
using BankOps.Web.Models.Banking.DTOs;

namespace BankOps.Web.Services;

public interface IBankingTransactionService
{
    Task<TransactionResponse> DepositAsync(
        DepositRequest request,
        string? userId);

    Task<TransactionResponse> WithdrawAsync(
        WithdrawalRequest request,
        string? userId);

    Task<TransactionResponse> TransferAsync(
        TransferRequest request,
        string? userId);
}
