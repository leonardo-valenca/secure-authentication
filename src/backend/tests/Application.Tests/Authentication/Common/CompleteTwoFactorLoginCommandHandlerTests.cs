using Application.Abstractions.Persistence;
using Application.Abstractions.Security;
using Application.Authentication.Commands;
using Domain.Authentication;
using Domain.Common;
using Domain.Users;
using NSubstitute;

namespace Application.Tests.Authentication.Commands;

public class CompleteTwoFactorLoginCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly ITokenHasher _tokenHasher = Substitute.For<ITokenHasher>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CompleteTwoFactorLoginCommandHandler _sut;

    public CompleteTwoFactorLoginCommandHandlerTests()
    {
        _sut = new CompleteTwoFactorLoginCommandHandler(_userRepository, _jwtTokenGenerator, _tokenHasher, _refreshTokenRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_InvalidCode_ReturnsTwoFactorCodeInvalidWithoutIssuingTokens()
    {
        var userId = Guid.NewGuid();
        _userRepository.VerifyTwoFactorCodeAsync(userId, "000000", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<User>(UserErrors.TwoFactorCodeInvalid));

        var result = await _sut.Handle(new CompleteTwoFactorLoginCommand(userId, "000000"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.TwoFactorCodeInvalid, result.Error);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCode_IssuesTokensAndPersistsRefreshToken()
    {
        var user = User.Create(Email.Create("user@example.com").Value);
        _userRepository.VerifyTwoFactorCodeAsync(user.Id, "123456", Arg.Any<CancellationToken>())
            .Returns(Result.Success(user));
        var accessToken = new AccessToken("jwt-value", DateTime.UtcNow.AddMinutes(15));
        _jwtTokenGenerator.Generate(user).Returns(accessToken);
        _tokenHasher.Hash(Arg.Any<string>()).Returns("hashed-refresh-token");

        var result = await _sut.Handle(new CompleteTwoFactorLoginCommand(user.Id, "123456"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("jwt-value", result.Value.AccessToken);
        Assert.Equal(user.Id, result.Value.User.Id);
        _refreshTokenRepository.Received(1).Add(Arg.Is<RefreshToken>(rt => rt.TokenHash == "hashed-refresh-token" && rt.UserId == user.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
