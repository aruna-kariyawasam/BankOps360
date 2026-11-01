namespace BankOps.Web.Models.Banking.DTOs;

public record TokenResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    int ExpiresInSeconds,
    IReadOnlyList<string> Roles
);