
using BankOps.Web.Data;
using BankOps.Web.Models.Banking;
using BankOps.Web.Models.Banking.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankOps.Web.Controllers;

[Authorize(Roles = "Administrator,Teller")]
public class CustomersController : Controller
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

    // GET: /Customers
    public async Task<IActionResult> Index(string? search)
    {
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

        var customers = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new CustomerListItemViewModel
            {
                Id = c.Id,
                CustomerNumber = c.CustomerNumber,
                FullName = c.FirstName + " " + c.LastName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                IsActive = c.IsActive,
                CreatedAtUtc = c.CreatedAtUtc
            })
            .ToListAsync();

        ViewBag.Search = search;

        return View(customers);
    }

    // GET: /Customers/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View("Form", new CustomerFormViewModel());
    }

    // POST: /Customers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View("Form", model);

        var customer = new Customer
        {
            CustomerNumber =
                $"CUS{Guid.NewGuid():N}".ToUpperInvariant()[..15],

            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            Email = model.Email?.Trim(),
            PhoneNumber = model.PhoneNumber?.Trim(),
            Address = model.Address?.Trim(),
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

            ModelState.AddModelError(
                string.Empty,
                "Unable to register the customer. Please try again.");

            return View("Form", model);
        }

        TempData["SuccessMessage"] =
            $"Customer {customer.CustomerNumber} registered successfully.";

        return RedirectToAction(nameof(Index));
    }

    // GET: /Customers/Details/1
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CustomerDetailsViewModel
            {
                Id = c.Id,
                CustomerNumber = c.CustomerNumber,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                Address = c.Address,
                IsActive = c.IsActive,
                CreatedAtUtc = c.CreatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (customer == null)
            return NotFound();

        return View(customer);
    }

    // GET: /Customers/Edit/1
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null)
            return NotFound();

        var model = new CustomerFormViewModel
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber,
            Address = customer.Address
        };

        return View("Form", model);
    }

    // POST: /Customers/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomerFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View("Form", model);

        if (model.Id == null)
            return BadRequest();

        var customer = await _context.Customers.FindAsync(model.Id.Value);

        if (customer == null)
            return NotFound();

        customer.FirstName = model.FirstName.Trim();
        customer.LastName = model.LastName.Trim();
        customer.Email = model.Email?.Trim();
        customer.PhoneNumber = model.PhoneNumber?.Trim();
        customer.Address = model.Address?.Trim();

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Customer information updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    // POST: /Customers/Deactivate/1
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null)
            return NotFound();

        customer.IsActive = false;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Customer deactivated successfully.";

        return RedirectToAction(nameof(Index));
    }
}
