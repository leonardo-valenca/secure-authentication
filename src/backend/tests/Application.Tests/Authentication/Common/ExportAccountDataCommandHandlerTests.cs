using Application.Abstractions.Persistence;
using Application.Authentication.Commands;
using Domain.Users;
using NSubstitute;

namespace Application.Tests.Authentication.Commands;

public class ExportAccountDataCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ExportAccountDataCommandHandler _sut;

    public ExportAccountDataCommandHandlerTests()
    {
        _sut = new ExportAccountDataCommandHandler(_userRepository);
    }

    [Fact]
    public async Task Handle_UnknownUserId_ReturnsAccountNotFound()
    {
        var userId = Guid.NewGuid();
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.Handle(new ExportAccountDataCommand(userId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.AccountNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_KnownUserId_ReturnsExportWithMatchingFields()
    {
        var email = Email.Create("export-test@example.com").Value;
        var createdAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var user = User.FromPersistence(Guid.NewGuid(), email, createdAtUtc);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(new ExportAccountDataCommand(user.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.Id);
        Assert.Equal(email.Value, result.Value.Email);
        Assert.Equal(createdAtUtc, result.Value.CreatedAtUtc);
    }
}
