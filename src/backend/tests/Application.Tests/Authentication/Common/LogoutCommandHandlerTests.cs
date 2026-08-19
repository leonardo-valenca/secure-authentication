using Application.Abstractions.Persistence;
using Application.Abstractions.Security;
using Application.Authentication.Commands;
using Domain.Authentication;
using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests.Authentication.Commands;

public class LogoutCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly ITokenHasher _tokenHasher = Substitute.For<ITokenHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly LogoutCommandHandler _sut;

    public LogoutCommandHandlerTests()
    {
        _sut = new LogoutCommandHandler(_refreshTokenRepository, _tokenHasher, _unitOfWork, NullLogger<LogoutCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_EmptyToken_SucceedsWithoutTouchingRepository()
    {
        var result = await _sut.Handle(new LogoutCommand(""), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _refreshTokenRepository.DidNotReceive().GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownToken_SucceedsWithoutSaving()
    {
        _tokenHasher.Hash(Arg.Any<string>()).Returns("hash");
        _refreshTokenRepository.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        var result = await _sut.Handle(new LogoutCommand("plain-text"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ActiveToken_RevokesAndSaves()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", TimeSpan.FromDays(30));
        _tokenHasher.Hash(Arg.Any<string>()).Returns("hash");
        _refreshTokenRepository.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns(token);

        var result = await _sut.Handle(new LogoutCommand("plain-text"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(token.IsRevoked);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
