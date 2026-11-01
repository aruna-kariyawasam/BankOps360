
using System.Security.Cryptography;
using BankOps.Web.Data;
using BankOps.Web.Models.Banking;
using BankOps.Web.Models.Banking.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;

namespace BankOps.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class BankAccountsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BankAccountsController> _logger;

    public BankAccountsController(
        ApplicationDbContext context,
        ILogger<BankAccountsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/bankaccounts
    [HttpGet]
    [Authorize(Roles = "Administrator,Teller,ComplianceOfficer")]
    public async Task<ActionResult<BankAccountListResponse>> GetAccounts(
        [FromQuery] int? customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.BankAccounts
            .AsNoTracking()
            .Include(a => a.Customer)
            .AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(
                a => a.CustomerId == customerId.Value);
        }

        var totalRecords = await query.CountAsync();

        var accounts = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new BankAccountResponse(
                a.Id,
                a.AccountNumber,
                a.CustomerId,
                a.Customer.FirstName + " " + a.Customer.LastName,
                a.AccountType,
                a.Status,
                a.Currency,
                a.CurrentBalance,
                a.CreatedAtUtc))
            .ToListAsync();

        return Ok(new BankAccountListResponse(
            page,
            pageSize,
            totalRecords,
            accounts));
    }

    // GET: api/bankaccounts/1
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Administrator,Teller,ComplianceOfficer")]
    public async Task<ActionResult<BankAccountResponse>> GetAccount(int id)
    {
        var account = await _context.BankAccounts
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new BankAccountResponse(
                a.Id,
                a.AccountNumber,
                a.CustomerId,
                a.Customer.FirstName + " " + a.Customer.LastName,
                a.AccountType,
                a.Status,
                a.Currency,
                a.CurrentBalance,
                a.CreatedAtUtc))
            .FirstOrDefaultAsync();

        if (account == null)
            return NotFound(new
            {
                message = "Bank account not found."
            });

        return Ok(account);
    }

    // POST: api/bankaccounts
    [HttpPost]
    [Authorize(Roles = "Administrator,Teller")]
    public async Task<ActionResult<BankAccountResponse>> CreateAccount(
        CreateBankAccountRequest request)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c =>
                c.Id == request.CustomerId &&
                c.IsActive);

        if (customer == null)
        {
            return BadRequest(new
            {
                message = "An active customer was not found."
            });
        }

        if (!Enum.IsDefined(
                typeof(AccountType), request.AccountType))
        {
            return BadRequest(new
            {
                message = "Invalid account type."
            });
        }

        var account = new BankAccount
        {
            AccountNumber = await GenerateAccountNumberAsync(),
            CustomerId = customer.Id,
            AccountType = request.AccountType,
            Status = AccountStatus.Active,
            Currency = "LKR",
            CurrentBalance = 0m,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.BankAccounts.Add(account);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Bank account creation failed.");

            return Conflict(new
            {
                message = "Unable to create the bank account."
            });
        }

        _logger.LogInformation(
            "Account {AccountNumber} created for customer {CustomerId}.",
            account.AccountNumber,
            customer.Id);

        var response = new BankAccountResponse(
            account.Id,
            account.AccountNumber,
            account.CustomerId,
            customer.FirstName + " " + customer.LastName,
            account.AccountType,
            account.Status,
            account.Currency,
            account.CurrentBalance,
            account.CreatedAtUtc);

        return CreatedAtAction(
            nameof(GetAccount),
            new { id = account.Id },
            response);
    }

    private async Task<string> GenerateAccountNumberAsync()
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            var number =
                $"10{RandomNumberGenerator.GetInt32(
                    1_000_000_000, 2_000_000_000)}";

            var exists = await _context.BankAccounts
                .AnyAsync(a => a.AccountNumber == number);

            if (!exists)
                return number;
        }

        throw new InvalidOperationException(
            "Unable to generate a unique account number.");
    }
}
