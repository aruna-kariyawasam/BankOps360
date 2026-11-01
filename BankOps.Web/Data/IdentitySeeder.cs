
using Microsoft.AspNetCore.Identity;
using BankOps.Web.Models.Banking;
using Microsoft.EntityFrameworkCore;
namespace BankOps.Web.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<IdentityUser>>();

        // 1. Create employee roles
        string[] roles =
        {
            "Administrator",
            "Teller",
            "ComplianceOfficer"
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(
                    new IdentityRole(role));

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create role {role}: " +
                        string.Join("; ", result.Errors.Select(
                            error => error.Description)));
                }
            }
        }

        // 2. Read development administrator credentials
        var adminEmail = configuration["SeedAdmin:Email"];
        var adminPassword = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) ||
            string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException(
                "Development administrator credentials are missing. " +
                "Configure SeedAdmin in User Secrets.");
        }

        // 3. Find or create administrator
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(
                adminUser, adminPassword);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Administrator creation failed: " +
                    string.Join("; ", createResult.Errors.Select(
                        error => error.Description)));
            }
        }

        // 4. Assign administrator role
        if (!await userManager.IsInRoleAsync(
                adminUser, "Administrator"))
        {
            var roleResult = await userManager.AddToRoleAsync(
                adminUser, "Administrator");

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Administrator role assignment failed: " +
                    string.Join("; ", roleResult.Errors.Select(
                        error => error.Description)));
            }
        }
        // 5. Seed the default LKR cash ledger account
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var cashLedgerExists = await dbContext.GeneralLedgerAccounts
            .AnyAsync(x => x.Code == "CASH-LKR");

        if (!cashLedgerExists)
        {
            dbContext.GeneralLedgerAccounts.Add(
                new GeneralLedgerAccount
                {
                    Code = "CASH-LKR",
                    Name = "Cash on Hand - LKR",
                    Currency = "LKR",
                    IsActive = true
                });

            await dbContext.SaveChangesAsync();
        }

        // Seed external clearing payable account
        var clearingLedgerExists = await dbContext.GeneralLedgerAccounts
            .AnyAsync(x => x.Code == "CLEARING-LKR");

        if (!clearingLedgerExists)
        {
            dbContext.GeneralLedgerAccounts.Add(
                new GeneralLedgerAccount
                {
                    Code = "CLEARING-LKR",
                    Name = "External Clearing Payable - LKR",
                    Currency = "LKR",
                    IsActive = true
                });

            await dbContext.SaveChangesAsync();
        }
    }
}
