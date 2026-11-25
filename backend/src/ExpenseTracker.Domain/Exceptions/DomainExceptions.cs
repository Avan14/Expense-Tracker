namespace ExpenseTracker.Domain.Exceptions;

public abstract class DomainException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public class NotFoundException(string message) : DomainException(message, 404);

public class BadRequestException(string message) : DomainException(message, 400);

public class ConflictException(string message) : DomainException(message, 409);

public class ForbiddenException(string message) : DomainException(message, 403);

public class UnauthorizedException(string message) : DomainException(message, 401);
