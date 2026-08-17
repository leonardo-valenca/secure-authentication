using Application.Abstractions.Persistence;
using Application.Abstractions.Security;
using Domain.Common;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Application.Authentication.Commands
{
    public sealed class LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ITokenHasher tokenHasher,
        IUnitOfWork unitOfWork,
        ILogger<LogoutCommandHandler> logger) : IRequestHandler<LogoutCommand, Result>
    {
        public async ValueTask<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return Result.Success();

            var tokenHash = tokenHasher.Hash(request.RefreshToken);
            var existingToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

            if (existingToken is null || existingToken.IsRevoked)
                return Result.Success();

            existingToken.Revoke();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.UserLoggedOut(existingToken.UserId);

            return Result.Success();
        }
    }
}
