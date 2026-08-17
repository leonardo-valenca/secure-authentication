using Application.Abstractions.Notifications;
using Application.Abstractions.Persistence;
using Domain.Common;
using Domain.Users;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed class ResendConfirmationEmailCommandHandler(IUserRepository userRepository, IEmailSender emailSender)
        : IRequestHandler<ResendConfirmationEmailCommand, Result>
    {
        public async ValueTask<Result> Handle(ResendConfirmationEmailCommand request, CancellationToken cancellationToken)
        {
            var emailResult = Email.Create(request.Email);
            if (emailResult.IsFailure)
                return Result.Success();

            var token = await userRepository.GenerateEmailConfirmationTokenAsync(emailResult.Value, cancellationToken);
            if (token is not null)
                await emailSender.SendEmailConfirmationEmailAsync(emailResult.Value.Value, token, cancellationToken);

            // Always succeeds, whether or not the account exists (or is already confirmed), same
            // reasoning as ForgotPasswordCommandHandler.
            return Result.Success();
        }
    }
}
