using System.Security.Cryptography;
using Application.Abstractions.Persistence;
using Application.Abstractions.Security;
using Application.Authentication.Responses;
using Domain.Authentication;
using Domain.Common;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Application.Authentication.Commands
{
    public sealed class RefreshCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        ITokenHasher tokenHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork,
        ILogger<RefreshCommandHandler> logger) : IRequestHandler<RefreshCommand, Result<LoginResult>>
    {
        public async ValueTask<Result<LoginResult>> Handle(RefreshCommand request, CancellationToken cancellationToken)
        {
            var tokenHash = tokenHasher.Hash(request.RefreshToken);
            var existingToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

            if (existingToken is null)
                return Result.Failure<LoginResult>(RefreshTokenErrors.NotFound);

            if (existingToken.IsRevoked)
            {
                // A revoked token being presented again means it was stolen and already rotated by
                // its rightful owner (or by an earlier attacker), kill every active session for this user.
                await RefreshTokenSessionRevoker.RevokeAllActiveAsync(refreshTokenRepository, unitOfWork, existingToken.UserId, cancellationToken);
                logger.RefreshTokenReuseDetected(existingToken.UserId);
                return Result.Failure<LoginResult>(RefreshTokenErrors.ReuseDetected);
            }

            if (existingToken.IsExpired)
                return Result.Failure<LoginResult>(RefreshTokenErrors.Expired);

            var user = await userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);
            if (user is null)
                return Result.Failure<LoginResult>(RefreshTokenErrors.NotFound);

            var newRefreshTokenPlainText = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var newRefreshTokenHash = tokenHasher.Hash(newRefreshTokenPlainText);
            var newRefreshToken = RefreshToken.Create(user.Id, newRefreshTokenHash, AuthenticationConstants.RefreshTokenLifetime);

            existingToken.Revoke(newRefreshToken.Id);
            refreshTokenRepository.Add(newRefreshToken);

            var accessToken = jwtTokenGenerator.Generate(user);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new LoginResult(
                new AuthenticationResponse(user.Id, user.Email.Value),
                accessToken.Value,
                accessToken.ExpiresAtUtc,
                newRefreshTokenPlainText,
                newRefreshToken.ExpiresAtUtc);
        }
    }
}
