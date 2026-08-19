using Application.Abstractions.Persistence;
using Application.Authentication.Commands;
using Domain.Common;
using Domain.Users;
using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests.Authentication.Commands;

public class ConfirmEmailCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ConfirmEmailCommandHandler _sut;

    public ConfirmEmailCommandHandlerTests()
    {
        _sut = new ConfirmEmailCommandHandler(_userRepository, NullLogger<ConfirmEmailCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_InvalidEmailFormat_ReturnsInvalidConfirmationTokenWithoutTouchingRepository()
    {
        var result = await _sut.Handle(new ConfirmEmailCommand("not-an-email", "token"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.InvalidConfirmationToken, result.Error);
        await _userRepository.DidNotReceive().ConfirmEmailAsync(Arg.Any<Email>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RepositoryFails_PropagatesError()
    {
        _userRepository.ConfirmEmailAsync(Arg.Any<Email>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(UserErrors.InvalidConfirmationToken));

        var result = await _sut.Handle(new ConfirmEmailCommand("user@example.com", "bad-token"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.InvalidConfirmationToken, result.Error);
    }

    [Fact]
    public async Task Handle_ValidRequest_Succeeds()
    {
        _userRepository.ConfirmEmailAsync(Arg.Any<Email>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _sut.Handle(new ConfirmEmailCommand("user@example.com", "good-token"), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
