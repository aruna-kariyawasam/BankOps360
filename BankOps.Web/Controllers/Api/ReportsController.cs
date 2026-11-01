
using System.Data;
using BankOps.Web.Data;
using BankOps.Web.Models.Banking.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;

namespace BankOps.Web.Controllers.Api;

[ApiController]
[Route("api/reports")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator,ComplianceOfficer")]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        ApplicationDbContext context,
        ILogger<ReportsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/reports/ledger-reconciliation
    [HttpGet("ledger-reconciliation")]
    public async Task<ActionResult<IReadOnlyList<LedgerReconciliationResult>>>
        GetLedgerReconciliation(
            [FromQuery] DateTime fromUtc,
            [FromQuery] DateTime toUtc,
            [FromQuery] bool exceptionsOnly = true,
            CancellationToken cancellationToken = default)
    {
        if (fromUtc >= toUtc ||
            toUtc - fromUtc > TimeSpan.FromDays(31))
        {
            return BadRequest(new
            {
                message = "Invalid date range. Maximum range is 31 days."
            });
        }

        try
        {
            var connection = _context.Database.GetDbConnection();

            var openedHere = connection.State != ConnectionState.Open;

            if (openedHere)
                await connection.OpenAsync(cancellationToken);

            try
            {
                await using var command = connection.CreateCommand();

                command.CommandText = "dbo.usp_ReconcileLedger";
                command.CommandType = CommandType.StoredProcedure;

                var fromParameter = command.CreateParameter();
                fromParameter.ParameterName = "@FromUtc";
                fromParameter.DbType = DbType.DateTime2;
                fromParameter.Value = fromUtc;
                command.Parameters.Add(fromParameter);

                var toParameter = command.CreateParameter();
                toParameter.ParameterName = "@ToUtc";
                toParameter.DbType = DbType.DateTime2;
                toParameter.Value = toUtc;
                command.Parameters.Add(toParameter);

                var results = new List<LedgerReconciliationResult>();

                await using var reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    results.Add(new LedgerReconciliationResult
                    {
                        TransactionId = reader.GetInt64(
                            reader.GetOrdinal("TransactionId")),

                        TransactionReference = reader.GetString(
                            reader.GetOrdinal("TransactionReference")),

                        TransactionType = reader.GetInt32(
                            reader.GetOrdinal("TransactionType")),

                        TransactionStatus = reader.GetInt32(
                            reader.GetOrdinal("TransactionStatus")),

                        TransactionAmount = reader.GetDecimal(
                            reader.GetOrdinal("TransactionAmount")),

                        Currency = reader.GetString(
                            reader.GetOrdinal("Currency")),

                        CreatedAtUtc = reader.GetDateTime(
                            reader.GetOrdinal("CreatedAtUtc")),

                        TotalDebits = reader.GetDecimal(
                            reader.GetOrdinal("TotalDebits")),

                        TotalCredits = reader.GetDecimal(
                            reader.GetOrdinal("TotalCredits")),

                        Difference = reader.GetDecimal(
                            reader.GetOrdinal("Difference")),

                        EntryCount = reader.GetInt32(
                            reader.GetOrdinal("EntryCount")),

                        IsBalanced = reader.GetBoolean(
                            reader.GetOrdinal("IsBalanced"))
                    });
                }

                var finalResults = exceptionsOnly
                    ? results.Where(x => !x.IsBalanced).ToList()
                    : results;

                return Ok(finalResults);
            }
            finally
            {
                if (openedHere)
                    await connection.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ledger reconciliation failed.");

            return Problem(
                title: "Ledger reconciliation failed.",
                statusCode: 500);
        }
    }

    // GET: api/reports/transaction-summary
    [HttpGet("transaction-summary")]
    public async Task<ActionResult<IReadOnlyList<TransactionSummaryResult>>>
        GetTransactionSummary(
            [FromQuery] DateTime fromUtc,
            [FromQuery] DateTime toUtc,
            CancellationToken cancellationToken)
    {
        if (fromUtc >= toUtc ||
            toUtc - fromUtc > TimeSpan.FromDays(31))
        {
            return BadRequest(new
            {
                message = "Invalid date range. Maximum range is 31 days."
            });
        }

        try
        {
            var connection = _context.Database.GetDbConnection();

            var openedHere = connection.State != ConnectionState.Open;

            if (openedHere)
                await connection.OpenAsync(cancellationToken);

            try
            {
                await using var command = connection.CreateCommand();

                command.CommandText =
                    "dbo.usp_GetTransactionSummary";

                command.CommandType = CommandType.StoredProcedure;

                var fromParameter = command.CreateParameter();
                fromParameter.ParameterName = "@FromUtc";
                fromParameter.DbType = DbType.DateTime2;
                fromParameter.Value = fromUtc;
                command.Parameters.Add(fromParameter);

                var toParameter = command.CreateParameter();
                toParameter.ParameterName = "@ToUtc";
                toParameter.DbType = DbType.DateTime2;
                toParameter.Value = toUtc;
                command.Parameters.Add(toParameter);

                var results = new List<TransactionSummaryResult>();

                await using var reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    results.Add(new TransactionSummaryResult
                    {
                        TransactionType = reader.GetInt32(
                            reader.GetOrdinal("TransactionType")),

                        TransactionStatus = reader.GetInt32(
                            reader.GetOrdinal("TransactionStatus")),

                        TransactionCount = reader.GetInt64(
                            reader.GetOrdinal("TransactionCount")),

                        TotalAmount = reader.GetDecimal(
                            reader.GetOrdinal("TotalAmount"))
                    });
                }

                return Ok(results);
            }
            finally
            {
                if (openedHere)
                    await connection.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction summary failed.");

            return Problem(
                title: "Transaction summary failed.",
                statusCode: 500);
        }
    }
}
