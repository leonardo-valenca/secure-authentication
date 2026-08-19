using Application.Abstractions.Persistence;
using Application.Abstractions.Security;
using Application.Authentication.Commands;
using Application.Authentication.Responses;
using Domain.Authentication;
using Domain.Common;
using Domain.Users;
using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests.Authentication.Commands;

public class LoginCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly ITokenHasher _tokenHasher = Substitute.For<ITokenHasher>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly LoginCommandHandler _sut;

    public LoginCommandHandlerTests()
    {
        _sut = new LoginCommandHandler(_userRepository, _jwtTokenGenerator, _tokenHasher, _refreshTokenRepository, _unitOfWork, NullLogger<LoginCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsInvalidCredentials()
    {
        _userRepository.VerifyCredentialsAsync(Arg.Any<Email>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CredentialVerificationResult>(UserErrors.InvalidCredentials));
        var command = new LoginCommand("nobody@example.com", "whatever");

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task Handle_WrongPasswordOrLockedOut_ReturnsInvalidCredentials()
    {
        _userRepository.VerifyCredentialsAsync(Arg.Any<Email>(), "wrong", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CredentialVerificationResult>(UserErrors.InvalidCredentials));
        var command = new LoginCommand("user@example.com", "wrong");

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task Handle_UnconfirmedEmail_ReturnsEmailNotConfirmed()
    {
        _userRepository.VerifyCredentialsAsync(Arg.Any<Email>(), "correct", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CredentialVerificationResult>(UserErrors.EmailNotConfirmed));
        var command = new LoginCommand("user@example.com", "correct");

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.EmailNotConfirmed, result.Error);
    }

    [Fact]
    public async Task Handle_ValidCredentials_IssuesTokensAndPersistsRefreshToken()
    {
        var user = User.Create(Email.Create("user@example.com").Value);
        _userRepository.VerifyCredentialsAsync(Arg.Any<Email>(), "correct", Arg.Any<CancellationToken>())
            .Returns(Result.Success(new CredentialVerificationResult(user, RequiresTwoFactor: false)));
        var accessToken = new AccessToken("jwt-value", DateTime.UtcNow.AddMinutes(15));
        _jwtTokenGenerator.Generate(user).Returns(accessToken);
        _tokenHasher.Hash(Arg.Any<string>()).Returns("hashed-refresh-token");

        var command = new LoginCommand("user@example.com", "correct");

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var completed = Assert.IsType<LoginOutcome.Completed>(result.Value);
        Assert.Equal("jwt-value", completed.Result.AccessToken);
        Assert.Equal(user.Id, completed.Result.User.Id);
        Assert.False(string.IsNullOrEmpty(completed.Result.RefreshToken));
        _refreshTokenRepository.Received(1).Add(Arg.Is<RefreshToken>(rt => rt.TokenHash == "hashed-refresh-token" && rt.UserId == user.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TwoFactorEnabled_ReturnsRequiresTwoFactorWithoutIssuingTokens()
    {
        var user = User.Create(Email.Create("user@example.com").Value);
        _userRepository.VerifyCredentialsAsync(Arg.Any<Email>(), "correct", Arg.Any<CancellationToken>())
            .Returns(Result.Success(new CredentialVerificationResult(user, RequiresTwoFactor: true)));

        var command = new LoginCommand("user@example.com", "correct");

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var requiresTwoFactor = Assert.IsType<LoginOutcome.RequiresTwoFactor>(result.Value);
        Assert.Equal(user.Id, requiresTwoFactor.UserId);
        _refreshTokenRepository.DidNotReceive().Add(Arg.Any<RefreshToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
