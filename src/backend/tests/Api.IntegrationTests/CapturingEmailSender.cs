using Application.Abstractions.Notifications;

namespace Api.IntegrationTests;

/// <summary>Test double for IEmailSender that captures tokens instead of sending anything.</summary>
public sealed class CapturingEmailSender : IEmailSender
{
    public string? LastResetEmail { get; private set; }

    public string? LastResetToken { get; private set; }

    public string? LastConfirmationEmail { get; private set; }

    public string? LastConfirmationToken { get; private set; }

    public Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken cancellationToken)
    {
        LastResetEmail = email;
        LastResetToken = resetToken;
        return Task.CompletedTask;
    }

    public Task SendEmailConfirmationEmailAsync(string email, string confirmationToken, CancellationToken cancellationToken)
    {
        LastConfirmationEmail = email;
        LastConfirmationToken = confirmationToken;
        return Task.CompletedTask;
    }
}
