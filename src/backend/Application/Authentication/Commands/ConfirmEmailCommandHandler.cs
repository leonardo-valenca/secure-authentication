using Application.Abstractions.Persistence;
using Domain.Common;
using Domain.Users;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Application.Authentication.Commands
{
    public sealed class ConfirmEmailCommandHandler(
        IUserRepository userRepository,
        ILogger<ConfirmEmailCommandHandler> logger) : IRequestHandler<ConfirmEmailCommand, Result>
    {
        public async ValueTask<Result> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
        {
            var emailResult = Email.Create(request.Email);
            if (emailResult.IsFailure)
                return Result.Failure(UserErrors.InvalidConfirmationToken);

            var result = await userRepository.ConfirmEmailAsync(emailResult.Value, request.Token, cancellationToken);

            if (result.IsSuccess)
                logger.EmailConfirmed(emailResult.Value.Value);
            else
                logger.EmailConfirmationTokenInvalid(emailResult.Value.Value);

            return result;
        }
    }
}
