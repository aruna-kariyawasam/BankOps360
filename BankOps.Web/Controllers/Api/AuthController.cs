
using BankOps.Web.Models.Banking.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

namespace BankOps.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
[EnableRateLimiting("auth")]

public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _logger = logger;
    }

    // POST: api/auth/token
    [HttpPost("token")]
    public async Task<ActionResult<TokenResponse>> GenerateToken(
        LoginRequest request)
    {
        var email = request.Email.Trim();

        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        // Verify credentials and apply Identity lockout controls.
        var signInResult =
            await _signInManager.CheckPasswordSignInAsync(
                user,
                request.Password,
                lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        // Retrieve actual employee roles from Identity.
        var roles = await _userManager.GetRolesAsync(user);

        // Read JWT settings.
        var issuer = _configuration["Jwt:Issuer"]!;
        var audience = _configuration["Jwt:Audience"]!;
        var signingKey = _configuration["Jwt:SigningKey"]!;

        var accessTokenMinutes =
            _configuration.GetValue<int>(
                "Jwt:AccessTokenMinutes", 15);

        if (accessTokenMinutes < 1 ||
            accessTokenMinutes > 60)
        {
            throw new InvalidOperationException(
                "JWT expiry must be between 1 and 60 minutes.");
        }

        var key = new SymmetricSecurityKey(
            Convert.FromBase64String(signingKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(accessTokenMinutes);

        // Create token claims.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),

            new(JwtRegisteredClaimNames.Email,
                user.Email ?? email),

            new(JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()),

            new(ClaimTypes.NameIdentifier, user.Id),

            new(ClaimTypes.Name,
                user.UserName ?? email),

            new(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds()
                    .ToString(),
                ClaimValueTypes.Integer64)
        };

        // Include employee authorization roles.
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        var accessToken =
            new JwtSecurityTokenHandler().WriteToken(jwt);

        _logger.LogInformation(
            "JWT access token issued for employee {UserId}.",
            user.Id);

        return Ok(new TokenResponse(
            accessToken,
            "Bearer",
            expires,
            (int)(expires - now).TotalSeconds,
            roles.ToArray()
        ));
    }
}
