using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public static class RefreshTokenCleanup
    {
        /// <summary>
        /// Revoked-but-not-yet-expired tokens are kept for forensic value (e.g. reviewing a
        /// reuse-detection incident after the fact), only rows past their own expiry are purged,
        /// regardless of whether they were revoked or just naturally expired.
        /// </summary>
        public static Task<int> PurgeExpiredAsync(AppDbContext dbContext, CancellationToken cancellationToken)
        {
            return dbContext.RefreshTokens
                .Where(t => t.ExpiresAtUtc < DateTime.UtcNow)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
