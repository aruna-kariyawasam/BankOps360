
using BankOps.Web.Data;
using BankOps.Web.Models.Banking;
using BankOps.Web.Models.Banking.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BankOps.Web.Services;

public sealed class AmlMonitoringService : IAmlMonitoringService
{
    private const decimal HighValueThreshold = 1_000_000m;
    private const decimal DailyVolumeThreshold = 2_000_000m;
    private const int HighFrequencyThreshold = 5;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<AmlMonitoringService> _logger;

    public AmlMonitoringService(
        ApplicationDbContext context,
        ILogger<AmlMonitoringService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AmlAlertResponse>>
        EvaluateTransactionAsync(
            long transactionId,
            CancellationToken cancellationToken = default)
    {
        // Load the transaction being evaluated.
        var transaction = await _context.BankTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == transactionId,
                cancellationToken);

        if (transaction == null)
        {
            throw new BankingOperationException(
                404, "Transaction not found.");
        }

        // Only completed transactions are monitored.
        if (transaction.Status != TransactionStatus.Completed)
            return Array.Empty<AmlAlertResponse>();

        var accountIds = new[]
        {
            transaction.FromAccountId,
            transaction.ToAccountId
        }
        .Where(x => x.HasValue)
        .Select(x => x!.Value)
        .Distinct()
        .ToArray();

        if (accountIds.Length == 0)
            return Array.Empty<AmlAlertResponse>();

        var now = DateTime.UtcNow;
        var tenMinutesAgo = now.AddMinutes(-10);
        var oneDayAgo = now.AddHours(-24);

        // Load relevant completed transactions for involved accounts.
        var activity = await _context.BankTransactions
            .AsNoTracking()
            .Where(x =>
                x.Status == TransactionStatus.Completed &&
                x.CreatedAtUtc >= oneDayAgo &&
                (
                    (x.FromAccountId.HasValue &&
                     accountIds.Contains(x.FromAccountId.Value))
                    ||
                    (x.ToAccountId.HasValue &&
                     accountIds.Contains(x.ToAccountId.Value))
                ))
            .Select(x => new
            {
                x.Id,
                x.Amount,
                x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var rules = new List<(string Code, string Reason, int Score)>();

        // Rule 1: High-value transaction.
        if (transaction.Amount >= HighValueThreshold)
        {
            rules.Add((
                "AML-001",
                $"Single transaction amount of LKR {transaction.Amount:N2} " +
                $"meets the LKR {HighValueThreshold:N2} monitoring threshold.",
                80
            ));
        }

        // Rule 2: High transaction frequency within ten minutes.
        var recentTransactions = activity.Count(
            x => x.CreatedAtUtc >= tenMinutesAgo);

        if (recentTransactions >= HighFrequencyThreshold)
        {
            rules.Add((
                "AML-002",
                $"{recentTransactions} completed transactions involving " +
                "the associated accounts occurred within ten minutes.",
                70
            ));
        }

        // Rule 3: High cumulative transaction volume within 24 hours.
        var dailyVolume = activity.Sum(x => x.Amount);

        if (dailyVolume >= DailyVolumeThreshold)
        {
            rules.Add((
                "AML-003",
                $"Cumulative transaction volume of LKR {dailyVolume:N2} " +
                "involving the associated accounts reached the 24-hour threshold.",
                75
            ));
        }

        // Avoid creating duplicate alerts for a transaction and rule.
        var existingCodes = await _context.AmlAlerts
            .Where(x => x.BankTransactionId == transactionId)
            .Select(x => x.RuleCode)
            .ToListAsync(cancellationToken);

        var newAlerts = rules
            .Where(rule => !existingCodes.Contains(rule.Code))
            .Select(rule => new AmlAlert
            {
                BankTransactionId = transactionId,
                RuleCode = rule.Code,
                Reason = rule.Reason,
                RiskScore = rule.Score,
                Status = AmlAlertStatus.New,
                CreatedAtUtc = now
            })
            .ToList();

        if (newAlerts.Count > 0)
        {
            _context.AmlAlerts.AddRange(newAlerts);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "AML monitoring generated {Count} alert(s) for transaction {Reference}.",
                newAlerts.Count,
                transaction.TransactionReference);
        }

        return await GetAlertsForTransactionAsync(
            transactionId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<AmlAlertResponse>> GetAlertsAsync(
        AmlAlertStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.AmlAlerts
            .AsNoTracking()
            .Include(x => x.BankTransaction)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var alerts = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return alerts.Select(MapResponse).ToList();
    }

    public async Task<AmlAlertResponse?> GetAlertAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var alert = await _context.AmlAlerts
            .AsNoTracking()
            .Include(x => x.BankTransaction)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        return alert == null ? null : MapResponse(alert);
    }

    public async Task<AmlAlertResponse> ReviewAlertAsync(
        long id,
        ReviewAmlAlertRequest request,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var allowedStatuses = new[]
        {
            AmlAlertStatus.UnderReview,
            AmlAlertStatus.Cleared,
            AmlAlertStatus.Escalated,
            AmlAlertStatus.Closed
        };

        if (!allowedStatuses.Contains(request.Status))
        {
            throw new BankingOperationException(
                400, "Invalid investigation status.");
        }

        var alert = await _context.AmlAlerts
            .Include(x => x.BankTransaction)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (alert == null)
        {
            throw new BankingOperationException(
                404, "AML alert not found.");
        }

        if (alert.Status is AmlAlertStatus.Cleared
            or AmlAlertStatus.Closed)
        {
            throw new BankingOperationException(
                409, "This alert has already been finalized.");
        }

        var now = DateTime.UtcNow;

        alert.Status = request.Status;
        alert.ReviewNotes = request.ReviewNotes.Trim();
        alert.ReviewedByUserId = userId;
        alert.ReviewedAtUtc = now;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "AML_ALERT_REVIEWED",
            EntityName = "AmlAlert",
            EntityId = alert.Id.ToString(),
            Details =
                $"Rule={alert.RuleCode}; Status={request.Status}; " +
                $"Transaction={alert.BankTransaction.TransactionReference}",
            CreatedAtUtc = now
        });

        await _context.SaveChangesAsync(cancellationToken);

        return MapResponse(alert);
    }

    private async Task<IReadOnlyList<AmlAlertResponse>>
        GetAlertsForTransactionAsync(
            long transactionId,
            CancellationToken cancellationToken)
    {
        var alerts = await _context.AmlAlerts
            .AsNoTracking()
            .Include(x => x.BankTransaction)
            .Where(x => x.BankTransactionId == transactionId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return alerts.Select(MapResponse).ToList();
    }

    private static AmlAlertResponse MapResponse(AmlAlert alert)
    {
        return new AmlAlertResponse(
            alert.Id,
            alert.BankTransactionId,
            alert.BankTransaction.TransactionReference,
            alert.BankTransaction.Type,
            alert.BankTransaction.Amount,
            alert.BankTransaction.Currency,
            alert.RuleCode,
            alert.Reason,
            alert.RiskScore,
            alert.Status,
            alert.ReviewedByUserId,
            alert.ReviewNotes,
            alert.CreatedAtUtc,
            alert.ReviewedAtUtc);
    }
}
