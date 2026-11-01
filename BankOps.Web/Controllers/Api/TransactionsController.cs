
using System.Security.Claims;
using BankOps.Web.Models.Banking.DTOs;
using BankOps.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BankOps.Web.Controllers.Api;

[ApiController]
[Route("api/transactions")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator,Teller")]
public class TransactionsController : ControllerBase
{
    private readonly IBankingTransactionService _service;
    private readonly IAmlMonitoringService _amlMonitoring;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        IBankingTransactionService service,
        IAmlMonitoringService amlMonitoring,
        ILogger<TransactionsController> logger)
    {
        _service = service;
        _amlMonitoring = amlMonitoring;
        _logger = logger;
    }
    // POST: api/transactions/deposits
    [HttpPost("deposits")]
    public Task<ActionResult<TransactionResponse>> Deposit(
        DepositRequest request)
    {
        return ExecuteAsync(() =>
            _service.DepositAsync(request, GetUserId()));
    }

    // POST: api/transactions/withdrawals
    [HttpPost("withdrawals")]
    public Task<ActionResult<TransactionResponse>> Withdraw(
        WithdrawalRequest request)
    {
        return ExecuteAsync(() =>
            _service.WithdrawAsync(request, GetUserId()));
    }

    // POST: api/transactions/transfers
    [HttpPost("transfers")]
    public Task<ActionResult<TransactionResponse>> Transfer(
        TransferRequest request)
    {
        return ExecuteAsync(() =>
            _service.TransferAsync(request, GetUserId()));
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private async Task<ActionResult<TransactionResponse>> ExecuteAsync(
        Func<Task<TransactionResponse>> operation)
    {
        try
        {
            var result = await operation();

            // Evaluate only newly processed transactions.
            if (!result.IsDuplicateRequest)
            {
                try
                {
                    await _amlMonitoring.EvaluateTransactionAsync(result.Id);
                }
                catch (Exception ex)
                {
                    // The financial transaction has already committed.
                    // AML evaluation failure must not falsely reverse it.
                    _logger.LogError(
                        ex,
                        "AML evaluation failed for transaction {TransactionId}.",
                        result.Id);
                }
            }

            return Ok(result);
        }
        catch (BankingOperationException ex)
        {
            return StatusCode(
                ex.StatusCode,
                new { message = ex.Message });
        }
    }
}
