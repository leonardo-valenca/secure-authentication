using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence
{
    /// <summary>
    /// Without this, the RefreshTokens table grows forever in a long-running deployment, rotation
    /// and logout only mark tokens revoked, nothing ever deletes them. Runs once on startup, then
    /// on a fixed interval.
    /// </summary>
    internal sealed class RefreshTokenCleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<RefreshTokenCleanupBackgroundService> logger) : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);

            do
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var deleted = await RefreshTokenCleanup.PurgeExpiredAsync(dbContext, stoppingToken);
                    if (deleted > 0)
                        logger.LogInformation("Purged {Count} expired refresh token(s).", deleted);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Failed to purge expired refresh tokens.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
