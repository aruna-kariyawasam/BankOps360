
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Xml.Linq;

namespace BankOps.Web.Services;

public sealed class ClearingBankClient : IClearingBankClient
{
    private const string SoapNamespace =
        "http://schemas.xmlsoap.org/soap/envelope/";

    private const string ServiceNamespace =
        "urn:bankops:clearing:v1";

    private readonly HttpClient _httpClient;
    private readonly ILogger<ClearingBankClient> _logger;
    private readonly string _protocol;
    private readonly string _apiKey;

    public ClearingBankClient(
        HttpClient httpClient,
        ILogger<ClearingBankClient> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;

        _protocol = configuration["ClearingBank:Protocol"] ?? "Rest";

        _apiKey = configuration["ClearingBank:ApiKey"]
        ?? throw new InvalidOperationException(
            "Clearing bank API key is missing.");
    }

    public Task<ClearingPaymentResult> SubmitAsync(
        ClearingPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_protocol.Equals("Soap", StringComparison.OrdinalIgnoreCase))
        {
            return SubmitSoapAsync(request, cancellationToken);
        }

        if (_protocol.Equals("Rest", StringComparison.OrdinalIgnoreCase))
        {
            return SubmitRestAsync(request, cancellationToken);
        }

        throw new InvalidOperationException(
            $"Unsupported clearing protocol: {_protocol}");
    }

    private async Task<ClearingPaymentResult> SubmitRestAsync(
        ClearingPaymentRequest request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "api/clearing/payments")
                {
                    Content = JsonContent.Create(request)
                };

                httpRequest.Headers.Add("X-Clearing-Api-Key", _apiKey);

                using var response = await _httpClient.SendAsync(
                    httpRequest,
                    cancellationToken);

        var result = await response.Content
            .ReadFromJsonAsync<ClearingPaymentResponse>(
                cancellationToken: cancellationToken);

        if (result == null)
        {
            throw new HttpRequestException(
                "Clearing service returned an empty response.");
        }

        var rawResponse = System.Text.Json.JsonSerializer.Serialize(result);

        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            return new ClearingPaymentResult(
                false,
                result.ExternalReference,
                result.Message,
                rawResponse);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                "Clearing service returned an unexpected response.",
                null,
                response.StatusCode);
        }

        if (result.Decision != "Accepted")
        {
            throw new HttpRequestException(
                "Unknown clearing payment decision.");
        }

        return new ClearingPaymentResult(
            true,
            result.ExternalReference,
            result.Message,
            rawResponse);
    }

    private async Task<ClearingPaymentResult> SubmitSoapAsync(
        ClearingPaymentRequest request,
        CancellationToken cancellationToken)
    {
        XNamespace soap = SoapNamespace;
        XNamespace service = ServiceNamespace;

        var envelope = new XDocument(
            new XElement(soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", soap),
                new XAttribute(XNamespace.Xmlns + "clr", service),

                new XElement(soap + "Header"),

                new XElement(soap + "Body",
                    new XElement(service + "SubmitPayment",

                        new XElement(
                            service + "PaymentReference",
                            request.PaymentReference),

                        new XElement(
                            service + "IdempotencyKey",
                            request.IdempotencyKey),

                        new XElement(
                            service + "DestinationBankCode",
                            request.DestinationBankCode),

                        new XElement(
                            service + "DestinationAccountNumber",
                            request.DestinationAccountNumber),

                        new XElement(
                            service + "BeneficiaryName",
                            request.BeneficiaryName),

                        new XElement(
                            service + "Amount",
                            request.Amount.ToString(
                                CultureInfo.InvariantCulture)),

                        new XElement(
                            service + "Currency",
                            request.Currency)
                    )
                )
            )
        );

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "soap/ClearingService.asmx");

        httpRequest.Headers.Add("X-Clearing-Api-Key", _apiKey);

        httpRequest.Headers.TryAddWithoutValidation(
            "SOAPAction",
            "\"urn:bankops:clearing:v1/SubmitPayment\"");

        httpRequest.Content = new StringContent(
            envelope.ToString(SaveOptions.DisableFormatting),
            Encoding.UTF8,
            "text/xml");

        using var response = await _httpClient.SendAsync(
            httpRequest,
            cancellationToken);

        var rawResponse = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "SOAP clearing request failed with HTTP {StatusCode}.",
                (int)response.StatusCode);

            throw new HttpRequestException(
                "SOAP clearing service returned an error.",
                null,
                response.StatusCode);
        }

        var responseDocument = XDocument.Parse(rawResponse);

        string ReadValue(string elementName)
        {
            return responseDocument
                .Descendants()
                .FirstOrDefault(x =>
                    x.Name.LocalName == elementName)?.Value ?? "";
        }

        var decision = ReadValue("Decision");
        var externalReference = ReadValue("ExternalReference");
        var message = ReadValue("Message");

        if (string.IsNullOrWhiteSpace(decision))
        {
            var fault = ReadValue("faultstring");

            throw new HttpRequestException(
                string.IsNullOrWhiteSpace(fault)
                    ? "SOAP response did not contain a payment decision."
                    : $"SOAP fault: {fault}");
        }

        if (decision != "Accepted" && decision != "Rejected")
        {
            throw new HttpRequestException(
                "SOAP service returned an unknown payment decision.");
        }

        return new ClearingPaymentResult(
            decision == "Accepted",
            string.IsNullOrWhiteSpace(externalReference)
                ? null
                : externalReference,
            message,
            rawResponse);
    }
}
