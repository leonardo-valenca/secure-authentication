using Application.Abstractions.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Notifications
{
    /// <summary>
    /// Placeholder until a real email provider is wired up - logs instead of delivering, so the
    /// password-reset flow is fully testable end-to-end without external dependencies.
    /// </summary>
    internal sealed class LoggingEmailSender(IOptions<FrontendOptions> frontendOptions, ILogger<LoggingEmailSender> logger) : IEmailSender
    {
        public Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken cancellationToken)
        {
            var resetUrl = PasswordResetLinkBuilder.Build(frontendOptions.Value.BaseUrl, email, resetToken);

            if (resetUrl is null)
            {
                logger.LogWarning(
                    "No email provider is configured and Frontend:BaseUrl is unset - password reset token for {Email} was not delivered: {ResetToken}",
                    email,
                    resetToken);
            }
            else
            {
                logger.LogWarning(
                    "No email provider is configured - password reset link for {Email} was not delivered: {ResetUrl}",
                    email,
                    resetUrl);
            }

            return Task.CompletedTask;
        }

        public Task SendEmailConfirmationEmailAsync(string email, string confirmationToken, CancellationToken cancellationToken)
        {
            var confirmationUrl = EmailConfirmationLinkBuilder.Build(frontendOptions.Value.BaseUrl, email, confirmationToken);

            if (confirmationUrl is null)
            {
                logger.LogWarning(
                    "No email provider is configured and Frontend:BaseUrl is unset - email confirmation token for {Email} was not delivered: {ConfirmationToken}",
                    email,
                    confirmationToken);
            }
            else
            {
                logger.LogWarning(
                    "No email provider is configured - email confirmation link for {Email} was not delivered: {ConfirmationUrl}",
                    email,
                    confirmationUrl);
            }

            return Task.CompletedTask;
        }
    }
}
