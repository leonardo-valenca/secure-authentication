using Application.Abstractions.Persistence;
using Application.Authentication.Commands;
using Domain.Common;
using Domain.Users;
using NSubstitute;

namespace Application.Tests.Authentication.Commands;

public class GetTwoFactorStatusCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly GetTwoFactorStatusCommandHandler _sut;

    public GetTwoFactorStatusCommandHandlerTests()
    {
        _sut = new GetTwoFactorStatusCommandHandler(_userRepository);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_ReturnsWhateverTheRepositoryReports(bool enabled)
    {
        var userId = Guid.NewGuid();
        _userRepository.GetTwoFactorStatusAsync(userId, Arg.Any<CancellationToken>()).Returns(Result.Success(enabled));

        var result = await _sut.Handle(new GetTwoFactorStatusCommand(userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(enabled, result.Value);
    }

    [Fact]
    public async Task Handle_UnknownUserId_ReturnsAccountNotFound()
    {
        var userId = Guid.NewGuid();
        _userRepository.GetTwoFactorStatusAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(UserErrors.AccountNotFound));

        var result = await _sut.Handle(new GetTwoFactorStatusCommand(userId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.AccountNotFound, result.Error);
    }
}
