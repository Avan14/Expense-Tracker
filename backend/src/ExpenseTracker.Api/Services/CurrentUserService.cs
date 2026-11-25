using System.Security.Claims;
using ExpenseTracker.Application.Abstractions;
using ExpenseTracker.Domain.Exceptions;

namespace ExpenseTracker.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId
    {
        get
        {
            var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedException("User is not authenticated.");
            }

            return userId;
        }
    }

    public string? Email => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;
}
