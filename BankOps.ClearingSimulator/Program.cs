
using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 1. API KEY CONFIGURATION
// =====================================================

var expectedApiKey =
    builder.Configuration["ClearingSimulator:ApiKey"]
    ?? throw new InvalidOperationException(
        "Clearing simulator API key is missing.");

if (Encoding.UTF8.GetByteCount(expectedApiKey) < 32)
{
    throw new InvalidOperationException(
        "Clearing simulator API key must be at least 32 bytes.");
}

// =====================================================
// 2. IN-MEMORY PAYMENT STORE
// =====================================================

var processedPayments =
    new ConcurrentDictionary<string, StoredPayment>(
        StringComparer.Ordinal);

// =====================================================
// 3. AUTHENTICATION HELPER
// =====================================================

bool IsAuthorized(HttpRequest request)
{
    var provided =
        request.Headers["X-Clearing-Api-Key"].ToString();

    if (string.IsNullOrWhiteSpace(provided))
        return false;

    return CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(expectedApiKey),
        Encoding.UTF8.GetBytes(provided));
}

// =====================================================
// 4. PAYMENT VALIDATION HELPER
// =====================================================

string? ValidatePaymentRequest(ClearingPaymentRequest request)
{
    if (string.IsNullOrWhiteSpace(request.PaymentReference) ||
        request.PaymentReference.Length > 40)
    {
        return "Invalid payment reference.";
    }

    if (string.IsNullOrWhiteSpace(request.IdempotencyKey) ||
        request.IdempotencyKey.Length < 8 ||
        request.IdempotencyKey.Length > 64)
    {
        return "Invalid idempotency key.";
    }

    if (string.IsNullOrWhiteSpace(request.DestinationBankCode) ||
        request.DestinationBankCode.Length > 20)
    {
        return "Invalid destination bank code.";
    }

    if (string.IsNullOrWhiteSpace(request.DestinationAccountNumber) ||
        request.DestinationAccountNumber.Length > 30)
    {
        return "Invalid destination account number.";
    }

    if (string.IsNullOrWhiteSpace(request.BeneficiaryName) ||
        request.BeneficiaryName.Length > 150)
    {
        return "Invalid beneficiary name.";
    }

    if (request.Amount <= 0 ||
        decimal.Round(request.Amount, 2) != request.Amount)
    {
        return "Invalid payment amount.";
    }

    if (request.Currency != "LKR")
    {
        return "Unsupported currency.";
    }

    return null;
}

// =====================================================
// 5. APPLICATION
// =====================================================

var app = builder.Build();

// Health check
app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        Service = "BankOps Clearing Simulator",
        Status = "Healthy",
        TimestampUtc = DateTime.UtcNow
    });
});

// =====================================================
// 6. REST API - JSON PAYMENT PROCESSING
// =====================================================

app.MapPost("/api/clearing/payments", (
    HttpRequest httpRequest,
    ClearingPaymentRequest request) =>
{
    // API key authentication
    if (!IsAuthorized(httpRequest))
    {
        return Results.Unauthorized();
    }

    // Request validation
    var validationError = ValidatePaymentRequest(request);

    if (validationError != null)
    {
        return Results.BadRequest(new
        {
            Message = validationError
        });
    }

    // Demonstration rejection rule
    bool rejected = request.DestinationBankCode.Equals(
        "TEST-REJECT",
        StringComparison.OrdinalIgnoreCase);

    var response = new ClearingPaymentResponse(
        request.PaymentReference,
        rejected
            ? null
            : "CLR-" + Guid.NewGuid()
                .ToString("N")[..12].ToUpperInvariant(),
        rejected ? "Rejected" : "Accepted",
        rejected
            ? "Destination bank rejected the payment."
            : "Payment accepted by simulated clearing bank.",
        DateTime.UtcNow
    );

    var candidate = new StoredPayment(request, response);

    // Atomic duplicate request protection
    var stored = processedPayments.GetOrAdd(
        request.IdempotencyKey,
        candidate);

    if (stored.Request != request)
    {
        return Results.Conflict(new
        {
            Message =
                "Idempotency key already used for another payment."
        });
    }

    return stored.Response.Decision == "Accepted"
        ? Results.Ok(stored.Response)
        : Results.UnprocessableEntity(stored.Response);
});

// =====================================================
// 7. SOAP 1.1 - XML PAYMENT PROCESSING
// =====================================================

app.MapPost("/soap/ClearingService.asmx", async (
    HttpRequest httpRequest,
    CancellationToken cancellationToken) =>
{
    const string soapNamespace =
        "http://schemas.xmlsoap.org/soap/envelope/";

    const string serviceNamespace =
        "urn:bankops:clearing:v1";

    XNamespace soap = soapNamespace;
    XNamespace service = serviceNamespace;

    string SoapFault(string message)
    {
        var fault = new XDocument(
            new XElement(soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", soap),
                new XElement(soap + "Body",
                    new XElement(soap + "Fault",
                        new XElement(
                            "faultcode", "soap:Client"),

                        new XElement(
                            "faultstring", message)
                    )
                )
            )
        );

        return fault.ToString(
            SaveOptions.DisableFormatting);
    }

    // API key authentication
    if (!IsAuthorized(httpRequest))
    {
        return Results.Text(
            "Unauthorized",
            "text/plain",
            Encoding.UTF8,
            401);
    }

    // Content type validation
    if (httpRequest.ContentType == null ||
        !httpRequest.ContentType.StartsWith(
            "text/xml",
            StringComparison.OrdinalIgnoreCase))
    {
        return Results.Content(
            SoapFault("SOAP 1.1 requires text/xml."),
            "text/xml",
            Encoding.UTF8,
            415);
    }

    try
    {
        // Secure XML parsing:
        // DTD processing disabled, no external XML resolver.
        var xmlSettings = new XmlReaderSettings
        {
            Async = true,
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 65536
        };

        using var xmlReader = XmlReader.Create(
            httpRequest.Body,
            xmlSettings);

        var document = await XDocument.LoadAsync(
            xmlReader,
            LoadOptions.None,
            cancellationToken);

        // Require the expected SOAP envelope and namespace.
        var body = document.Root?
            .Element(soap + "Body");

        var operation = body?
            .Element(service + "SubmitPayment");

        if (operation == null)
        {
            return Results.Content(
                SoapFault(
                    "SubmitPayment SOAP operation is missing."),
                "text/xml",
                Encoding.UTF8,
                400);
        }

        string ReadValue(string name)
        {
            return operation
                .Element(service + name)?
                .Value.Trim() ?? string.Empty;
        }

        // Read XML request data
        var paymentReference =
            ReadValue("PaymentReference");

        var idempotencyKey =
            ReadValue("IdempotencyKey");

        var destinationBankCode =
            ReadValue("DestinationBankCode");

        var destinationAccountNumber =
            ReadValue("DestinationAccountNumber");

        var beneficiaryName =
            ReadValue("BeneficiaryName");

        var amountText =
            ReadValue("Amount");

        var currency =
            ReadValue("Currency");

        if (!decimal.TryParse(
            amountText,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var amount))
        {
            return Results.Content(
                SoapFault("Invalid payment amount."),
                "text/xml",
                Encoding.UTF8,
                400);
        }

        var request = new ClearingPaymentRequest(
            paymentReference,
            idempotencyKey,
            destinationBankCode,
            destinationAccountNumber,
            beneficiaryName,
            amount,
            currency
        );

        // Reuse the same payment validation.
        var validationError =
            ValidatePaymentRequest(request);

        if (validationError != null)
        {
            return Results.Content(
                SoapFault(validationError),
                "text/xml",
                Encoding.UTF8,
                400);
        }

        // Demonstration rejection rule
        bool rejected = destinationBankCode.Equals(
            "TEST-REJECT",
            StringComparison.OrdinalIgnoreCase);

        var result = new ClearingPaymentResponse(
            paymentReference,
            rejected
                ? null
                : "CLR-" + Guid.NewGuid()
                    .ToString("N")[..12].ToUpperInvariant(),
            rejected ? "Rejected" : "Accepted",
            rejected
                ? "Destination bank rejected the payment."
                : "Payment accepted by simulated clearing bank.",
            DateTime.UtcNow
        );

        var candidate = new StoredPayment(request, result);

        // Same idempotency store as the REST endpoint.
        var stored = processedPayments.GetOrAdd(
            idempotencyKey,
            candidate);

        if (stored.Request != request)
        {
            return Results.Content(
                SoapFault(
                    "Idempotency key already used for another payment."),
                "text/xml",
                Encoding.UTF8,
                409);
        }

        // Construct SOAP response.
        var responseDocument = new XDocument(
            new XElement(soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", soap),
                new XAttribute(XNamespace.Xmlns + "clr", service),

                new XElement(soap + "Body",
                    new XElement(
                        service + "SubmitPaymentResponse",

                        new XElement(
                            service + "PaymentReference",
                            stored.Response.PaymentReference),

                        new XElement(
                            service + "ExternalReference",
                            stored.Response.ExternalReference ?? ""),

                        new XElement(
                            service + "Decision",
                            stored.Response.Decision),

                        new XElement(
                            service + "Message",
                            stored.Response.Message),

                        new XElement(
                            service + "ProcessedAtUtc",
                            stored.Response.ProcessedAtUtc.ToString("O"))
                    )
                )
            )
        );

        return Results.Content(
            responseDocument.ToString(
                SaveOptions.DisableFormatting),
            "text/xml",
            Encoding.UTF8,
            200);
    }
    catch (XmlException)
    {
        return Results.Content(
            SoapFault("Malformed XML request."),
            "text/xml",
            Encoding.UTF8,
            400);
    }
});

// =====================================================
// 8. START SERVER
// =====================================================

app.Run("http://localhost:5081");

// =====================================================
// 9. PAYMENT CONTRACTS
// =====================================================

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

public sealed record StoredPayment(
    ClearingPaymentRequest Request,
    ClearingPaymentResponse Response
);
