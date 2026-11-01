
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
[Route("api/aml")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator,ComplianceOfficer")]
public class AmlController : ControllerBase
{
    private readonly IAmlMonitoringService _service;

    public AmlController(IAmlMonitoringService service)
    {
        _service = service;
    }

    // GET: api/aml/alerts
    [HttpGet("alerts")]
    public async Task<ActionResult<IReadOnlyList<AmlAlertResponse>>> GetAlerts(
        [FromQuery] AmlAlertStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetAlertsAsync(
            status, page, pageSize, cancellationToken);

        return Ok(result);
    }

    // GET: api/aml/alerts/1
    [HttpGet("alerts/{id:long}")]
    public async Task<ActionResult<AmlAlertResponse>> GetAlert(
        long id,
        CancellationToken cancellationToken)
    {
        var alert = await _service.GetAlertAsync(
            id, cancellationToken);

        if (alert == null)
            return NotFound(new { message = "AML alert not found." });

        return Ok(alert);
    }

    // POST: api/aml/transactions/1/evaluate
    [HttpPost("transactions/{transactionId:long}/evaluate")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> EvaluateTransaction(
        long transactionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var alerts = await _service.EvaluateTransactionAsync(
                transactionId, cancellationToken);

            return Ok(alerts);
        }
        catch (BankingOperationException ex)
        {
            return StatusCode(ex.StatusCode, new
            {
                message = ex.Message
            });
        }
    }

    // PATCH: api/aml/alerts/1/review
    [HttpPatch("alerts/{id:long}/review")]
    public async Task<ActionResult<AmlAlertResponse>> ReviewAlert(
        long id,
        ReviewAmlAlertRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var result = await _service.ReviewAlertAsync(
                id, request, userId, cancellationToken);

            return Ok(result);
        }
        catch (BankingOperationException ex)
        {
            return StatusCode(ex.StatusCode, new
            {
                message = ex.Message
            });
        }
    }
}
