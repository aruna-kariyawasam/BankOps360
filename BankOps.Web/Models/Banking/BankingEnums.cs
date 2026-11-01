namespace BankOps.Web.Models.Banking;

public enum AccountType
{
    Savings = 1,
    Current = 2
}

public enum AccountStatus
{
    Active = 1,
    Frozen = 2,
    Closed = 3
}

public enum TransactionType
{
    Deposit = 1,
    Withdrawal = 2,
    InternalTransfer = 3,
    ExternalPayment = 4
}

public enum TransactionStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Reversed = 4
}

public enum EntryType
{
    Debit = 1,
    Credit = 2
}

public enum PaymentStatus
{
    Pending = 1,
    Processing = 2,
    Successful = 3,
    Failed = 4,
    Reversed = 5
}

public enum AmlAlertStatus
{
    New = 1,
    UnderReview = 2,
    Cleared = 3,
    Escalated = 4,
    Closed = 5
}