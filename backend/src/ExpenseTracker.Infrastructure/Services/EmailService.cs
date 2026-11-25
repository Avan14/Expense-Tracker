using ExpenseTracker.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Infrastructure.Services;

public class EmailService(ILogger<EmailService> logger) : IEmailService
{
    public Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken cancellationToken = default)
    {
        // In production, integrate with SMTP / SendGrid / SES.
        logger.LogInformation(
            "Password reset requested for {Email}. Reset token: {ResetToken}",
            toEmail,
            resetToken);

        return Task.CompletedTask;
    }
}
