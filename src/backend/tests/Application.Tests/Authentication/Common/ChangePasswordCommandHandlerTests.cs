using Application.Abstractions.Persistence;
using Application.Authentication.Commands;
using Domain.Authentication;
using Domain.Common;
using Domain.Users;
using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests.Authentication.Commands;

public class ChangePasswordCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ChangePasswordCommandHandler _sut;

    public ChangePasswordCommandHandlerTests()
    {
        _sut = new ChangePasswordCommandHandler(_userRepository, _refreshTokenRepository, _unitOfWork, NullLogger<ChangePasswordCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ReturnsCurrentPasswordIncorrectWithoutRevokingSessions()
    {
        var userId = Guid.NewGuid();
        _userRepository.ChangePasswordAsync(userId, "WrongPass1", "NewPass1", Arg.Any<CancellationToken>())
            .Returns(Result.Failure(UserErrors.CurrentPasswordIncorrect));

        var result = await _sut.Handle(new ChangePasswordCommand(userId, "WrongPass1", "NewPass1"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.CurrentPasswordIncorrect, result.Error);
        await _refreshTokenRepository.DidNotReceive().GetActiveByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_RevokesActiveSessionsAndSucceeds()
    {
        var userId = Guid.NewGuid();
        var activeToken = RefreshToken.Create(userId, "hash", TimeSpan.FromDays(30));
        _userRepository.ChangePasswordAsync(userId, "OldPass1", "NewPass1", Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _refreshTokenRepository.GetActiveByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new[] { activeToken });

        var result = await _sut.Handle(new ChangePasswordCommand(userId, "OldPass1", "NewPass1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(activeToken.IsRevoked);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
