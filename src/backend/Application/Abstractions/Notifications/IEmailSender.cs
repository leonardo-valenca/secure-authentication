namespace Application.Abstractions.Notifications
{
    public interface IEmailSender
    {
        Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken cancellationToken);

        Task SendEmailConfirmationEmailAsync(string email, string confirmationToken, CancellationToken cancellationToken);
    }
}
