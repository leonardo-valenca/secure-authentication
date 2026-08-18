using Application.Abstractions.Notifications;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Notifications
{
    internal sealed class SmtpEmailSender(IOptions<SmtpOptions> smtpOptions, IOptions<FrontendOptions> frontendOptions) : IEmailSender
    {
        public async Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken cancellationToken)
        {
            var options = smtpOptions.Value;

            // A real email with no working link back to the app isn't a degraded experience,
            // it's a broken one, fail loudly instead of sending something unusable.
            var resetUrl = PasswordResetLinkBuilder.Build(frontendOptions.Value.BaseUrl, email, resetToken)
                ?? throw new InvalidOperationException("Frontend:BaseUrl must be configured to send password reset emails.");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = "Reset your password";
            message.Body = new TextPart("plain")
            {
                Text = $"""
                    We received a request to reset your password.

                    {resetUrl}

                    This link will expire soon. If you didn't request this, you can safely ignore this email.
                    """
            };

            await SendAsync(message, cancellationToken);
        }

        public async Task SendEmailConfirmationEmailAsync(string email, string confirmationToken, CancellationToken cancellationToken)
        {
            // Same reasoning as SendPasswordResetEmailAsync, a confirmation email with no working
            // link is broken, not degraded, so this fails loudly rather than sending it anyway.
            var confirmationUrl = EmailConfirmationLinkBuilder.Build(frontendOptions.Value.BaseUrl, email, confirmationToken)
                ?? throw new InvalidOperationException("Frontend:BaseUrl must be configured to send email confirmation emails.");

            var options = smtpOptions.Value;
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = "Confirm your email";
            message.Body = new TextPart("plain")
            {
                Text = $"""
                    Welcome! Please confirm your email address to finish setting up your account.

                    {confirmationUrl}

                    If you didn't create this account, you can safely ignore this email.
                    """
            };

            await SendAsync(message, cancellationToken);
        }

        private async Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
        {
            var options = smtpOptions.Value;

            using var client = new SmtpClient();
            await client.ConnectAsync(options.Host, options.Port, SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);

            if (!string.IsNullOrEmpty(options.Username))
            {
                if (string.IsNullOrEmpty(options.Password))
                    throw new InvalidOperationException("Smtp:Password must be configured when Smtp:Username is set.");

                await client.AuthenticateAsync(options.Username, options.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);
        }
    }
}
