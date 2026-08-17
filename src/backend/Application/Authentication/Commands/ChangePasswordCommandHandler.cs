using Application.Abstractions.Persistence;
using Domain.Common;
using Domain.Users;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Application.Authentication.Commands
{
    public sealed class ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        ILogger<ChangePasswordCommandHandler> logger) : IRequestHandler<ChangePasswordCommand, Result>
    {
        public async ValueTask<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var result = await userRepository.ChangePasswordAsync(request.UserId, request.CurrentPassword, request.NewPassword, cancellationToken);
            if (result.IsFailure)
            {
                if (result.Error == UserErrors.CurrentPasswordIncorrect)
                    logger.PasswordChangeFailed(request.UserId);

                return result;
            }

            // Forces re-authentication with the new password everywhere, including the calling
            // device, consistent with the reset-password flow rather than a lesser guarantee.
            await RefreshTokenSessionRevoker.RevokeAllActiveAsync(refreshTokenRepository, unitOfWork, request.UserId, cancellationToken);

            logger.PasswordChanged(request.UserId);

            return Result.Success();
        }
    }
}
