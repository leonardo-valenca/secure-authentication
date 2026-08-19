using Application.Abstractions.Notifications;
using Application.Abstractions.Persistence;
using Application.Authentication.Commands;
using Domain.Users;
using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests.Authentication.Commands;

public class ForgotPasswordCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly ForgotPasswordCommandHandler _sut;

    public ForgotPasswordCommandHandlerTests()
    {
        _sut = new ForgotPasswordCommandHandler(_userRepository, _emailSender, NullLogger<ForgotPasswordCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_UnknownEmail_SucceedsWithoutSendingEmail()
    {
        _userRepository.GeneratePasswordResetTokenAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns((string?)null);

        var result = await _sut.Handle(new ForgotPasswordCommand("unknown@example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _emailSender.DidNotReceive().SendPasswordResetEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_KnownEmail_SendsResetEmail()
    {
        _userRepository.GeneratePasswordResetTokenAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns("reset-token");

        var result = await _sut.Handle(new ForgotPasswordCommand("known@example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _emailSender.Received(1).SendPasswordResetEmailAsync("known@example.com", "reset-token", Arg.Any<CancellationToken>());
    }
}
