using Application.Abstractions.Persistence;
using Application.Authentication.Commands;
using Domain.Authentication;
using Domain.Common;
using Domain.Users;
using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests.Authentication.Commands;

public class ResetPasswordCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ResetPasswordCommandHandler _sut;

    public ResetPasswordCommandHandlerTests()
    {
        _sut = new ResetPasswordCommandHandler(_userRepository, _refreshTokenRepository, _unitOfWork, NullLogger<ResetPasswordCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_InvalidEmailFormat_ReturnsInvalidResetTokenWithoutTouchingRepository()
    {
        var result = await _sut.Handle(new ResetPasswordCommand("not-an-email", "token", "NewPass1"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.InvalidResetToken, result.Error);
        await _userRepository.DidNotReceive().ResetPasswordAsync(
            Arg.Any<Email>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RepositoryFails_PropagatesErrorWithoutRevokingSessions()
    {
        _userRepository.ResetPasswordAsync(Arg.Any<Email>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<Guid>(UserErrors.InvalidResetToken));

        var result = await _sut.Handle(new ResetPasswordCommand("user@example.com", "bad-token", "NewPass1"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.InvalidResetToken, result.Error);
        await _refreshTokenRepository.DidNotReceive().GetActiveByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_RevokesActiveSessionsAndSucceeds()
    {
        var userId = Guid.NewGuid();
        var activeToken = RefreshToken.Create(userId, "hash", TimeSpan.FromDays(30));
        _userRepository.ResetPasswordAsync(Arg.Any<Email>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(userId));
        _refreshTokenRepository.GetActiveByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new[] { activeToken });

        var result = await _sut.Handle(new ResetPasswordCommand("user@example.com", "good-token", "NewPass1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(activeToken.IsRevoked);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
