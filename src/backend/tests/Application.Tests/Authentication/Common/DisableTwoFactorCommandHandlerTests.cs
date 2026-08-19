using Application.Abstractions.Persistence;
using Application.Authentication.Commands;
using Domain.Common;
using Domain.Users;
using NSubstitute;

namespace Application.Tests.Authentication.Commands;

public class DisableTwoFactorCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly DisableTwoFactorCommandHandler _sut;

    public DisableTwoFactorCommandHandlerTests()
    {
        _sut = new DisableTwoFactorCommandHandler(_userRepository);
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ReturnsCurrentPasswordIncorrect()
    {
        var userId = Guid.NewGuid();
        _userRepository.DisableTwoFactorAsync(userId, "WrongPass1", Arg.Any<CancellationToken>())
            .Returns(Result.Failure(UserErrors.CurrentPasswordIncorrect));

        var result = await _sut.Handle(new DisableTwoFactorCommand(userId, "WrongPass1"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.CurrentPasswordIncorrect, result.Error);
    }

    [Fact]
    public async Task Handle_CorrectCurrentPassword_Succeeds()
    {
        var userId = Guid.NewGuid();
        _userRepository.DisableTwoFactorAsync(userId, "CorrectPass1", Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _sut.Handle(new DisableTwoFactorCommand(userId, "CorrectPass1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
