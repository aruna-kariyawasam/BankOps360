
CREATE OR ALTER PROCEDURE dbo.usp_ReconcileLedger
    @FromUtc DATETIME2(7),
    @ToUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromUtc >= @ToUtc
    BEGIN
        THROW 50001, 'Invalid reconciliation date range.', 1;
    END;

    SELECT
        bt.Id AS TransactionId,
        bt.TransactionReference,
        bt.Type AS TransactionType,
        bt.Status AS TransactionStatus,
        bt.Amount AS TransactionAmount,
        bt.Currency,
        bt.CreatedAtUtc,

        CAST(
            COALESCE(
                SUM(CASE
                    WHEN te.EntryType = 1 THEN te.Amount
                    ELSE 0
                END), 0
            ) AS DECIMAL(18,2)
        ) AS TotalDebits,

        CAST(
            COALESCE(
                SUM(CASE
                    WHEN te.EntryType = 2 THEN te.Amount
                    ELSE 0
                END), 0
            ) AS DECIMAL(18,2)
        ) AS TotalCredits,

        CAST(
            COALESCE(
                SUM(CASE
                    WHEN te.EntryType = 1 THEN te.Amount
                    ELSE 0
                END), 0
            ) -
            COALESCE(
                SUM(CASE
                    WHEN te.EntryType = 2 THEN te.Amount
                    ELSE 0
                END), 0
            ) AS DECIMAL(18,2)
        ) AS Difference,

        CONVERT(INT, COUNT_BIG(te.Id)) AS EntryCount,

        CAST(
            CASE
                WHEN COUNT_BIG(te.Id) >= 2

                AND SUM(CASE
                    WHEN te.EntryType = 1 THEN te.Amount
                    ELSE 0
                END)
                =
                SUM(CASE
                    WHEN te.EntryType = 2 THEN te.Amount
                    ELSE 0
                END)

                AND SUM(CASE
                    WHEN te.EntryType = 1 THEN te.Amount
                    ELSE 0
                END) = bt.Amount

                THEN 1
                ELSE 0
            END AS BIT
        ) AS IsBalanced

    FROM dbo.BankTransactions bt

    LEFT JOIN dbo.TransactionEntries te
        ON bt.Id = te.BankTransactionId

    WHERE bt.CreatedAtUtc >= @FromUtc
      AND bt.CreatedAtUtc < @ToUtc

    GROUP BY
        bt.Id,
        bt.TransactionReference,
        bt.Type,
        bt.Status,
        bt.Amount,
        bt.Currency,
        bt.CreatedAtUtc

    ORDER BY bt.CreatedAtUtc DESC;
END;
GO


CREATE OR ALTER PROCEDURE dbo.usp_GetTransactionSummary
    @FromUtc DATETIME2(7),
    @ToUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromUtc >= @ToUtc
    BEGIN
        THROW 50002, 'Invalid reporting date range.', 1;
    END;

    SELECT
        bt.Type AS TransactionType,
        bt.Status AS TransactionStatus,

        COUNT_BIG(*) AS TransactionCount,

        CAST(
            SUM(bt.Amount) AS DECIMAL(18,2)
        ) AS TotalAmount

    FROM dbo.BankTransactions bt

    WHERE bt.CreatedAtUtc >= @FromUtc
      AND bt.CreatedAtUtc < @ToUtc

    GROUP BY
        bt.Type,
        bt.Status

    ORDER BY
        bt.Type,
        bt.Status;
END;
GO
