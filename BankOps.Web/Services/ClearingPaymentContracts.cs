namespace BankOps.Web.Services;

public sealed record ClearingPaymentRequest(
    string PaymentReference,
    string IdempotencyKey,
    string DestinationBankCode,
    string DestinationAccountNumber,
    string BeneficiaryName,
    decimal Amount,
    string Currency
);

public sealed record ClearingPaymentResponse(
    string PaymentReference,
    string? ExternalReference,
    string Decision,
    string Message,
    DateTime ProcessedAtUtc
);

public sealed record ClearingPaymentResult(
    bool IsAccepted,
    string? ExternalReference,
    string Message,
    string RawResponse
);