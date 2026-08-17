using Application.Abstractions.Persistence;
using Domain.Common;
using Domain.Users;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Application.Authentication.Commands
{
    public sealed class ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        ILogger<ResetPasswordCommandHandler> logger) : IRequestHandler<ResetPasswordCommand, Result>
    {
        public async ValueTask<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var emailResult = Email.Create(request.Email);
            if (emailResult.IsFailure)
                return Result.Failure(UserErrors.InvalidResetToken);

            var resetResult = await userRepository.ResetPasswordAsync(emailResult.Value, request.Token, request.NewPassword, cancellationToken);
            if (resetResult.IsFailure)
            {
                if (resetResult.Error == UserErrors.InvalidResetToken)
                    logger.PasswordResetTokenInvalid(emailResult.Value.Value);

                return Result.Failure(resetResult.Error);
            }

            // A password reset is often a response to a compromised account, any session
            // established before the reset (e.g. an attacker's) must not survive it.
            await RefreshTokenSessionRevoker.RevokeAllActiveAsync(refreshTokenRepository, unitOfWork, resetResult.Value, cancellationToken);

            logger.PasswordResetCompleted(resetResult.Value);

            return Result.Success();
        }
    }
}
