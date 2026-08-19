using Domain.Authentication;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.IntegrationTests;

public sealed class RefreshTokenCleanupTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RefreshTokenCleanupTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PurgeExpiredAsync_DeletesOnlyTokensPastTheirExpiry()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppIdentityUser>>();

        var email = $"cleanup-{Guid.NewGuid():N}@example.com";
        var identityUser = new AppIdentityUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            CreatedAtUtc = DateTime.UtcNow,
        };
        var createResult = await userManager.CreateAsync(identityUser, "StrongPass1!");
        Assert.True(createResult.Succeeded);

        var expiredToken = RefreshToken.Create(identityUser.Id, $"expired-{Guid.NewGuid()}", TimeSpan.FromSeconds(-1));
        var revokedButNotYetExpiredToken = RefreshToken.Create(identityUser.Id, $"revoked-{Guid.NewGuid()}", TimeSpan.FromDays(30));
        revokedButNotYetExpiredToken.Revoke();
        var activeToken = RefreshToken.Create(identityUser.Id, $"active-{Guid.NewGuid()}", TimeSpan.FromDays(30));

        dbContext.RefreshTokens.AddRange(expiredToken, revokedButNotYetExpiredToken, activeToken);
        await dbContext.SaveChangesAsync();

        var deletedCount = await RefreshTokenCleanup.PurgeExpiredAsync(dbContext, CancellationToken.None);

        Assert.Equal(1, deletedCount);
        Assert.False(await dbContext.RefreshTokens.AnyAsync(t => t.Id == expiredToken.Id));
        // Revoked doesn't mean deleted; it's kept until its own expiry for forensic value.
        Assert.True(await dbContext.RefreshTokens.AnyAsync(t => t.Id == revokedButNotYetExpiredToken.Id));
        Assert.True(await dbContext.RefreshTokens.AnyAsync(t => t.Id == activeToken.Id));
    }
}
