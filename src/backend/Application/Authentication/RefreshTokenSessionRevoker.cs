using Application.Abstractions.Persistence;

namespace Application.Authentication
{
    /// <summary>
    /// Kills every active session for a user, used wherever a credential-invalidating event
    /// (refresh token reuse, password reset, password change) must not leave old sessions usable.
    /// </summary>
    internal static class RefreshTokenSessionRevoker
    {
        public static async Task RevokeAllActiveAsync(
            IRefreshTokenRepository refreshTokenRepository,
            IUnitOfWork unitOfWork,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var activeTokens = await refreshTokenRepository.GetActiveByUserIdAsync(userId, cancellationToken);
            if (activeTokens.Count == 0)
                return;

            foreach (var token in activeTokens)
                token.Revoke();

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
