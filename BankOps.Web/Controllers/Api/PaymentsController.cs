
using System.Security.Claims;
using BankOps.Web.Models.Banking;
using BankOps.Web.Models.Banking.DTOs;
using BankOps.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;

namespace BankOps.Web.Controllers.Api;

[ApiController]
[Route("api/payments")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentProcessingService _service;

    public PaymentsController(IPaymentProcessingService service)
    {
        _service = service;
    }

    // POST: api/payments
    [HttpPost]
    [Authorize(Roles = "Administrator,Teller")]
    public async Task<ActionResult<PaymentResponse>> CreatePayment(
        CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var result = await _service.ProcessPaymentAsync(
                request, userId, cancellationToken);

            return result.Status switch
            {
                PaymentStatus.Processing =>
                    AcceptedAtAction(
                        nameof(GetPayment),
                        new { paymentReference = result.PaymentReference },
                        result),

                PaymentStatus.Failed =>
                    UnprocessableEntity(result),

                _ => Ok(result)
            };
        }
        catch (BankingOperationException ex)
        {
            return StatusCode(ex.StatusCode, new
            {
                message = ex.Message
            });
        }
    }

    // GET: api/payments/{paymentReference}
    [HttpGet("{paymentReference}")]
    [Authorize(Roles = "Administrator,Teller,ComplianceOfficer")]
    public async Task<ActionResult<PaymentResponse>> GetPayment(
        string paymentReference,
        CancellationToken cancellationToken)
    {
        var payment = await _service.GetPaymentAsync(
            paymentReference, cancellationToken);

        if (payment == null)
            return NotFound(new { message = "Payment not found." });

        return Ok(payment);
    }
}
