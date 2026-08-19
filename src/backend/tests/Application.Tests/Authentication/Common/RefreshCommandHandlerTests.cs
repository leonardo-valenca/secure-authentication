using Application.Abstractions.Persistence;
using Application.Abstractions.Security;
using Application.Authentication.Commands;
using Domain.Authentication;
using Domain.Users;
using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests.Authentication.Commands;

public class RefreshCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITokenHasher _tokenHasher = Substitute.For<ITokenHasher>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RefreshCommandHandler _sut;

    public RefreshCommandHandlerTests()
    {
        _sut = new RefreshCommandHandler(_refreshTokenRepository, _userRepository, _tokenHasher, _jwtTokenGenerator, _unitOfWork, NullLogger<RefreshCommandHandler>.Instance);
        _tokenHasher.Hash(Arg.Any<string>()).Returns(callInfo => "hash-of-" + callInfo.Arg<string>());
    }

    [Fact]
    public async Task Handle_UnknownToken_ReturnsNotFound()
    {
        _refreshTokenRepository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        var result = await _sut.Handle(new RefreshCommand("plain-text"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RefreshTokenErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_RevokedToken_RevokesAllActiveTokensForUserAndReturnsReuseDetected()
    {
        var userId = Guid.NewGuid();
        var revokedToken = RefreshToken.Create(userId, "hash-of-plain-text", TimeSpan.FromDays(30));
        revokedToken.Revoke();

        var otherActiveToken = RefreshToken.Create(userId, "some-other-hash", TimeSpan.FromDays(30));

        _refreshTokenRepository.GetByTokenHashAsync("hash-of-plain-text", Arg.Any<CancellationToken>()).Returns(revokedToken);
        _refreshTokenRepository.GetActiveByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns([otherActiveToken]);

        var result = await _sut.Handle(new RefreshCommand("plain-text"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RefreshTokenErrors.ReuseDetected, result.Error);
        Assert.True(otherActiveToken.IsRevoked);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsExpired()
    {
        var expiredToken = RefreshToken.Create(Guid.NewGuid(), "hash-of-plain-text", TimeSpan.FromSeconds(-1));
        _refreshTokenRepository.GetByTokenHashAsync("hash-of-plain-text", Arg.Any<CancellationToken>()).Returns(expiredToken);

        var result = await _sut.Handle(new RefreshCommand("plain-text"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RefreshTokenErrors.Expired, result.Error);
    }

    [Fact]
    public async Task Handle_ValidToken_RotatesAndReturnsNewTokens()
    {
        var user = User.Create(Email.Create("user@example.com").Value);
        var activeToken = RefreshToken.Create(user.Id, "hash-of-plain-text", TimeSpan.FromDays(30));

        _refreshTokenRepository.GetByTokenHashAsync("hash-of-plain-text", Arg.Any<CancellationToken>()).Returns(activeToken);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _jwtTokenGenerator.Generate(user).Returns(new AccessToken("new-jwt", DateTime.UtcNow.AddMinutes(15)));

        var result = await _sut.Handle(new RefreshCommand("plain-text"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("new-jwt", result.Value.AccessToken);
        Assert.True(activeToken.IsRevoked);
        _refreshTokenRepository.Received(1).Add(Arg.Is<RefreshToken>(rt => rt.UserId == user.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
