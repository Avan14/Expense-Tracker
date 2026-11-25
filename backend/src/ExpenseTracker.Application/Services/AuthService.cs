using ExpenseTracker.Application.Abstractions;
using ExpenseTracker.Application.DTOs.Auth;
using ExpenseTracker.Application.Services.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Services;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    IAppDbContext dbContext,
    IJwtTokenService jwtTokenService,
    IEmailService emailService) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existingUser = await userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser != null)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = normalizedEmail,
            Email = normalizedEmail,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
            EmailConfirmed = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            throw new BadRequestException(errors);
        }

        await userManager.AddToRoleAsync(user, "User");

        return await IssueAuthTokensAsync(user, ipAddress, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        return await IssueAuthTokensAsync(user, ipAddress, cancellationToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new BadRequestException("Refresh token is required.");
        }

        var tokenHash = jwtTokenService.HashToken(request.RefreshToken);
        var existingToken = await dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (existingToken == null)
        {
            throw new UnauthorizedException("Invalid refresh token.");
        }

        if (existingToken.IsRevoked)
        {
            // Token reuse detected: revoke all active tokens for this user
            await RevokeAllUserTokensAsync(existingToken.UserId, ipAddress, "Attempted reuse of revoked refresh token", cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Refresh token has been revoked.");
        }

        if (existingToken.IsExpired)
        {
            throw new UnauthorizedException("Refresh token has expired.");
        }

        var (newRawToken, newTokenHash, newExpiresAt) = jwtTokenService.GenerateRefreshToken();

        existingToken.RevokedAtUtc = DateTime.UtcNow;
        existingToken.RevokedByIp = ipAddress;
        existingToken.ReplacedByTokenHash = newTokenHash;
        existingToken.ReasonRevoked = "Replaced by rotation";

        var newRefreshToken = new RefreshToken
        {
            UserId = existingToken.UserId,
            TokenHash = newTokenHash,
            ExpiresAtUtc = newExpiresAt,
            CreatedByIp = ipAddress,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.RefreshTokens.Add(newRefreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var roles = await userManager.GetRolesAsync(existingToken.User);
        var (accessToken, accessExpiresAt) = jwtTokenService.GenerateAccessToken(existingToken.User, roles);

        return new AuthResponse(
            accessToken,
            accessExpiresAt,
            newRawToken,
            newExpiresAt,
            MapUserProfile(existingToken.User, roles));
    }

    public async Task RevokeTokenAsync(RevokeTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new BadRequestException("Refresh token is required.");
        }

        var tokenHash = jwtTokenService.HashToken(request.RefreshToken);
        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (token == null || !token.IsActive)
        {
            throw new BadRequestException("Token is invalid or already revoked.");
        }

        token.RevokedAtUtc = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReasonRevoked = "Revoked by user";
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user == null)
        {
            // Do not reveal user existence
            return;
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        await emailService.SendPasswordResetEmailAsync(user.Email!, resetToken, cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user == null)
        {
            throw new BadRequestException("Invalid password reset request.");
        }

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new BadRequestException(errors);
        }

        await RevokeAllUserTokensAsync(user.Id, null, "Password reset", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserProfileResponse> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found.");

        var roles = await userManager.GetRolesAsync(user);
        return MapUserProfile(user, roles);
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found.");

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
        user.MonthlySavingsTarget = request.MonthlySavingsTarget;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new BadRequestException(errors);
        }

        var roles = await userManager.GetRolesAsync(user);
        return MapUserProfile(user, roles);
    }

    private async Task<AuthResponse> IssueAuthTokensAsync(ApplicationUser user, string? ipAddress, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, accessExpiresAt) = jwtTokenService.GenerateAccessToken(user, roles);
        var (rawRefreshToken, tokenHash, refreshExpiresAt) = jwtTokenService.GenerateRefreshToken();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAtUtc = refreshExpiresAt,
            CreatedByIp = ipAddress,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            accessExpiresAt,
            rawRefreshToken,
            refreshExpiresAt,
            MapUserProfile(user, roles));
    }

    private async Task RevokeAllUserTokensAsync(Guid userId, string? ipAddress, string reason, CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAtUtc == null && rt.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.ReasonRevoked = reason;
        }
    }

    private static UserProfileResponse MapUserProfile(ApplicationUser user, IList<string> roles) =>
        new(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.CurrencyCode,
            user.MonthlySavingsTarget,
            roles.ToList(),
            user.CreatedAtUtc);
}
