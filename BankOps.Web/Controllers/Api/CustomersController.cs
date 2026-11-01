
using BankOps.Web.Data;
using BankOps.Web.Models.Banking;
using BankOps.Web.Models.Banking.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace BankOps.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes =
    JwtBearerDefaults.AuthenticationScheme)]
public class CustomersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        ApplicationDbContext context,
        ILogger<CustomersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/customers
    [HttpGet]
    [Authorize(Roles = "Administrator,Teller,ComplianceOfficer")]
    public async Task<ActionResult<CustomerListResponse>> GetCustomers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(c =>
                c.CustomerNumber.Contains(search) ||
                c.FirstName.Contains(search) ||
                c.LastName.Contains(search) ||
                (c.Email != null && c.Email.Contains(search)));
        }

        var totalRecords = await query.CountAsync();

        var customers = await query
            .OrderBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerResponse(
                c.Id,
                c.CustomerNumber,
                c.FirstName,
                c.LastName,
                c.Email,
                c.PhoneNumber,
                c.Address,
                c.CreatedAtUtc,
                c.IsActive))
            .ToListAsync();

        return Ok(new CustomerListResponse(
            page,
            pageSize,
            totalRecords,
            customers));
    }

    // GET: api/customers/1
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Administrator,Teller,ComplianceOfficer")]
    public async Task<ActionResult<CustomerResponse>> GetCustomer(int id)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CustomerResponse(
                c.Id,
                c.CustomerNumber,
                c.FirstName,
                c.LastName,
                c.Email,
                c.PhoneNumber,
                c.Address,
                c.CreatedAtUtc,
                c.IsActive))
            .FirstOrDefaultAsync();

        if (customer == null)
            return NotFound(new { message = "Customer not found." });

        return Ok(customer);
    }

    // POST: api/customers
    [HttpPost]
    [Authorize(Roles = "Administrator,Teller")]
    public async Task<ActionResult<CustomerResponse>> CreateCustomer(
        CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            CustomerNumber =
                $"CUS{Guid.NewGuid():N}".ToUpperInvariant()[..15],

            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Address = request.Address?.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        _context.Customers.Add(customer);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Customer creation failed.");
            return Conflict(new
            {
                message = "Customer could not be created."
            });
        }

        var response = new CustomerResponse(
            customer.Id,
            customer.CustomerNumber,
            customer.FirstName,
            customer.LastName,
            customer.Email,
            customer.PhoneNumber,
            customer.Address,
            customer.CreatedAtUtc,
            customer.IsActive);

        _logger.LogInformation(
            "Customer {CustomerNumber} created.",
            customer.CustomerNumber);

        return CreatedAtAction(
            nameof(GetCustomer),
            new { id = customer.Id },
            response);
    }

    // PUT: api/customers/1
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator,Teller")]
    public async Task<IActionResult> UpdateCustomer(
        int id,
        CreateCustomerRequest request)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null)
            return NotFound(new { message = "Customer not found." });

        customer.FirstName = request.FirstName.Trim();
        customer.LastName = request.LastName.Trim();
        customer.Email = request.Email?.Trim();
        customer.PhoneNumber = request.PhoneNumber?.Trim();
        customer.Address = request.Address?.Trim();

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PATCH: api/customers/1/deactivate
    [HttpPatch("{id:int}/deactivate")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeactivateCustomer(int id)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null)
            return NotFound();

        customer.IsActive = false;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
