using System.Security.Cryptography;
using Application.Abstractions.Persistence;
using Application.Abstractions.Security;
using Application.Authentication.Commands;
using Application.Authentication.Responses;
using Domain.Authentication;
using Domain.Users;

namespace Application.Authentication
{
    /// <summary>
    /// Issues a fresh access/refresh token pair for an already-verified user, shared by
    /// LoginCommandHandler (password alone was enough) and CompleteTwoFactorLoginCommandHandler
    /// (password plus a TOTP/recovery code) so both end at the exact same token-issuance logic
    /// rather than two copies that could drift.
    /// </summary>
    internal static class LoginTokenIssuer
    {
        public static async Task<LoginResult> IssueAsync(
            User user,
            IJwtTokenGenerator jwtTokenGenerator,
            ITokenHasher tokenHasher,
            IRefreshTokenRepository refreshTokenRepository,
            IUnitOfWork unitOfWork,
            CancellationToken cancellationToken)
        {
            var accessToken = jwtTokenGenerator.Generate(user);

            var refreshTokenPlainText = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refreshTokenHash = tokenHasher.Hash(refreshTokenPlainText);
            var refreshToken = RefreshToken.Create(user.Id, refreshTokenHash, AuthenticationConstants.RefreshTokenLifetime);

            refreshTokenRepository.Add(refreshToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new LoginResult(
                new AuthenticationResponse(user.Id, user.Email.Value),
                accessToken.Value,
                accessToken.ExpiresAtUtc,
                refreshTokenPlainText,
                refreshToken.ExpiresAtUtc);
        }
    }
}
