using Application.Abstractions.Persistence;
using Domain.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    internal sealed class RefreshTokenRepository(AppDbContext dbContext) : IRefreshTokenRepository
    {
        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
        {
            return dbContext.RefreshTokens.SingleOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);
        }

        public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await dbContext.RefreshTokens
                .Where(r => r.UserId == userId && r.RevokedAtUtc == null && r.ExpiresAtUtc > DateTime.UtcNow)
                .ToListAsync(cancellationToken);
        }

        public void Add(RefreshToken refreshToken)
        {
            dbContext.RefreshTokens.Add(refreshToken);
        }
    }
}
