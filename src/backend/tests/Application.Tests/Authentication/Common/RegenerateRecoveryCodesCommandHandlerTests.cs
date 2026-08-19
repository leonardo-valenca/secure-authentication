using Application.Abstractions.Persistence;
using Application.Authentication.Commands;
using Domain.Common;
using Domain.Users;
using NSubstitute;

namespace Application.Tests.Authentication.Commands;

public class RegenerateRecoveryCodesCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly RegenerateRecoveryCodesCommandHandler _sut;

    public RegenerateRecoveryCodesCommandHandlerTests()
    {
        _sut = new RegenerateRecoveryCodesCommandHandler(_userRepository);
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ReturnsCurrentPasswordIncorrect()
    {
        var userId = Guid.NewGuid();
        _userRepository.RegenerateRecoveryCodesAsync(userId, "WrongPass1", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<string>>(UserErrors.CurrentPasswordIncorrect));

        var result = await _sut.Handle(new RegenerateRecoveryCodesCommand(userId, "WrongPass1"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.CurrentPasswordIncorrect, result.Error);
    }

    [Fact]
    public async Task Handle_CorrectCurrentPassword_ReturnsNewRecoveryCodes()
    {
        var userId = Guid.NewGuid();
        IReadOnlyList<string> recoveryCodes = ["new-code-1", "new-code-2"];
        _userRepository.RegenerateRecoveryCodesAsync(userId, "CorrectPass1", Arg.Any<CancellationToken>())
            .Returns(Result.Success(recoveryCodes));

        var result = await _sut.Handle(new RegenerateRecoveryCodesCommand(userId, "CorrectPass1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(recoveryCodes, result.Value);
    }
}
