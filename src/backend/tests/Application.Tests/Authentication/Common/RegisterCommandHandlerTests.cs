using Application.Abstractions.Notifications;
using Application.Abstractions.Persistence;
using Application.Authentication.Commands;
using Domain.Common;
using Domain.Users;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Authentication.Commands;

public class RegisterCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly ILogger<RegisterCommandHandler> _logger = Substitute.For<ILogger<RegisterCommandHandler>>();
    private readonly RegisterCommandHandler _sut;

    public RegisterCommandHandlerTests()
    {
        _sut = new RegisterCommandHandler(_userRepository, _emailSender, _logger);
    }

    [Fact]
    public async Task Handle_InvalidEmailFormat_ReturnsFailureWithoutTouchingRepository()
    {
        var command = new RegisterCommand("not-an-email", "StrongPass1");

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.EmailInvalidFormat, result.Error);
        await _userRepository.DidNotReceive().ExistsByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailAlreadyRegistered_ReturnsFailureWithoutCreating()
    {
        _userRepository.ExistsByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(true);
        var command = new RegisterCommand("existing@example.com", "StrongPass1");

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.EmailAlreadyInUse, result.Error);
        await _userRepository.DidNotReceive().CreateAsync(Arg.Any<Email>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CreateFailsWithDuplicateEmail_ReturnsEmailAlreadyInUse()
    {
        _userRepository.ExistsByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepository.CreateAsync(Arg.Any<Email>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<User>(UserErrors.EmailAlreadyInUse));
        var command = new RegisterCommand("race.condition@example.com", "StrongPass1");

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.EmailAlreadyInUse, result.Error);
    }

    [Fact]
    public async Task Handle_CreateFailsWithWeakPassword_ReturnsWeakPassword()
    {
        _userRepository.ExistsByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepository.CreateAsync(Arg.Any<Email>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<User>(UserErrors.WeakPassword));
        var command = new RegisterCommand("new.user@example.com", "weak");

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.WeakPassword, result.Error);
    }

    [Fact]
    public async Task Handle_NewUser_DelegatesCreationAndReturnsResponse()
    {
        _userRepository.ExistsByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);
        var createdUser = User.Create(Email.Create("new.user@example.com").Value);
        _userRepository.CreateAsync(Arg.Any<Email>(), "StrongPass1", Arg.Any<CancellationToken>()).Returns(Result.Success(createdUser));
        _userRepository.GenerateEmailConfirmationTokenAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns("confirmation-token");
        var command = new RegisterCommand("new.user@example.com", "StrongPass1");

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("new.user@example.com", result.Value.Email);
        Assert.Equal(createdUser.Id, result.Value.Id);
        await _userRepository.Received(1).CreateAsync(
            Arg.Is<Email>(e => e.Value == "new.user@example.com"), "StrongPass1", Arg.Any<CancellationToken>());
        await _emailSender.Received(1).SendEmailConfirmationEmailAsync("new.user@example.com", "confirmation-token", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConfirmationEmailSendFails_StillReturnsSuccess()
    {
        // The account was already created by this point, so a delivery failure (e.g. an SMTP
        // outage) must not turn into a registration failure the user can't recover from.
        _userRepository.ExistsByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);
        var createdUser = User.Create(Email.Create("new.user@example.com").Value);
        _userRepository.CreateAsync(Arg.Any<Email>(), "StrongPass1", Arg.Any<CancellationToken>()).Returns(Result.Success(createdUser));
        _userRepository.GenerateEmailConfirmationTokenAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns("confirmation-token");
        _emailSender.SendEmailConfirmationEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SMTP is unavailable"));
        var command = new RegisterCommand("new.user@example.com", "StrongPass1");

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(createdUser.Id, result.Value.Id);
    }
}
