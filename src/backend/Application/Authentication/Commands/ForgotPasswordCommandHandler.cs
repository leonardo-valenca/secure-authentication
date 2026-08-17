using Application.Abstractions.Notifications;
using Application.Abstractions.Persistence;
using Domain.Common;
using Domain.Users;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Application.Authentication.Commands
{
    public sealed class ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IEmailSender emailSender,
        ILogger<ForgotPasswordCommandHandler> logger) : IRequestHandler<ForgotPasswordCommand, Result>
    {
        public async ValueTask<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var emailResult = Email.Create(request.Email);
            if (emailResult.IsFailure)
                return Result.Success();

            logger.PasswordResetRequested(emailResult.Value.Value);

            var token = await userRepository.GeneratePasswordResetTokenAsync(emailResult.Value, cancellationToken);
            if (token is not null)
                await emailSender.SendPasswordResetEmailAsync(emailResult.Value.Value, token, cancellationToken);

            // Always succeeds, whether or not the account exists, the response must not leak
            // which emails are registered. The log line above still records every attempt
            // (existing account or not), that asymmetry between response and internal log is
            // what lets an operator spot someone probing many emails without exposing it externally.
            return Result.Success();
        }
    }
}
