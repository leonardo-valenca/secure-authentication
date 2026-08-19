using Domain.Authentication;

namespace Domain.Tests.Authentication;

public class RefreshTokenTests
{
    [Fact]
    public void Create_NewToken_IsActive()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromDays(1));

        Assert.False(token.IsRevoked);
        Assert.False(token.IsExpired);
        Assert.True(token.IsActive);
    }

    [Fact]
    public void Create_PastLifetime_IsExpired()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromSeconds(-1));

        Assert.True(token.IsExpired);
        Assert.False(token.IsActive);
    }

    [Fact]
    public void Revoke_SetsRevokedAtAndReplacement()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromDays(1));
        var replacementId = Guid.NewGuid();

        token.Revoke(replacementId);

        Assert.True(token.IsRevoked);
        Assert.False(token.IsActive);
        Assert.Equal(replacementId, token.ReplacedByTokenId);
    }
}
