using Application.Abstractions.Notifications;
using Application.Abstractions.Persistence;
using Application.Authentication.Commands;
using Domain.Users;
using NSubstitute;

namespace Application.Tests.Authentication.Commands;

public class ResendConfirmationEmailCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly ResendConfirmationEmailCommandHandler _sut;

    public ResendConfirmationEmailCommandHandlerTests()
    {
        _sut = new ResendConfirmationEmailCommandHandler(_userRepository, _emailSender);
    }

    [Fact]
    public async Task Handle_UnknownEmail_SucceedsWithoutSendingEmail()
    {
        _userRepository.GenerateEmailConfirmationTokenAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns((string?)null);

        var result = await _sut.Handle(new ResendConfirmationEmailCommand("unknown@example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _emailSender.DidNotReceive().SendEmailConfirmationEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_KnownEmail_SendsConfirmationEmail()
    {
        _userRepository.GenerateEmailConfirmationTokenAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns("confirmation-token");

        var result = await _sut.Handle(new ResendConfirmationEmailCommand("known@example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _emailSender.Received(1).SendEmailConfirmationEmailAsync("known@example.com", "confirmation-token", Arg.Any<CancellationToken>());
    }
}
