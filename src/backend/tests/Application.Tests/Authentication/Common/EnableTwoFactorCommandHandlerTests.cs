using Application.Abstractions.Persistence;
using Application.Authentication.Commands;
using Domain.Common;
using Domain.Users;
using NSubstitute;

namespace Application.Tests.Authentication.Commands;

public class EnableTwoFactorCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly EnableTwoFactorCommandHandler _sut;

    public EnableTwoFactorCommandHandlerTests()
    {
        _sut = new EnableTwoFactorCommandHandler(_userRepository);
    }

    [Fact]
    public async Task Handle_InvalidCode_ReturnsTwoFactorCodeInvalid()
    {
        var userId = Guid.NewGuid();
        _userRepository.EnableTwoFactorAsync(userId, "000000", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<string>>(UserErrors.TwoFactorCodeInvalid));

        var result = await _sut.Handle(new EnableTwoFactorCommand(userId, "000000"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.TwoFactorCodeInvalid, result.Error);
    }

    [Fact]
    public async Task Handle_ValidCode_ReturnsRecoveryCodes()
    {
        var userId = Guid.NewGuid();
        IReadOnlyList<string> recoveryCodes = ["code-1", "code-2"];
        _userRepository.EnableTwoFactorAsync(userId, "123456", Arg.Any<CancellationToken>())
            .Returns(Result.Success(recoveryCodes));

        var result = await _sut.Handle(new EnableTwoFactorCommand(userId, "123456"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(recoveryCodes, result.Value);
    }
}
