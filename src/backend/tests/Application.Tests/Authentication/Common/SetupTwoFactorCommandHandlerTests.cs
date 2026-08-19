using Application.Abstractions.Persistence;
using Application.Authentication.Commands;
using Application.Authentication.Responses;
using Domain.Common;
using NSubstitute;

namespace Application.Tests.Authentication.Commands;

public class SetupTwoFactorCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly SetupTwoFactorCommandHandler _sut;

    public SetupTwoFactorCommandHandlerTests()
    {
        _sut = new SetupTwoFactorCommandHandler(_userRepository);
    }

    [Fact]
    public async Task Handle_ReturnsWhateverTheRepositoryProduces()
    {
        var userId = Guid.NewGuid();
        var setup = new TwoFactorSetup("SHAREDKEY", "otpauth://totp/Issuer:user@example.com?secret=SHAREDKEY&issuer=Issuer&digits=6");
        _userRepository.GenerateTwoFactorSetupAsync(userId, Arg.Any<CancellationToken>()).Returns(Result.Success(setup));

        var result = await _sut.Handle(new SetupTwoFactorCommand(userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(setup, result.Value);
    }
}
