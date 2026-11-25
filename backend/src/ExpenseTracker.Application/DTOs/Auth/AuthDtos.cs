namespace ExpenseTracker.Application.DTOs.Auth;

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string CurrencyCode = "USD");

public record LoginRequest(string Email, string Password);

public record RefreshTokenRequest(string RefreshToken);

public record RevokeTokenRequest(string RefreshToken);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Token, string NewPassword);

public record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    UserProfileResponse User);

public record UserProfileResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string CurrencyCode,
    decimal? MonthlySavingsTarget,
    IReadOnlyList<string> Roles,
    DateTime CreatedAtUtc);

public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string CurrencyCode,
    decimal? MonthlySavingsTarget);
