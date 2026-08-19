using Application.Authentication.Commands;

namespace Application.Tests.Authentication.Commands;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var result = _validator.Validate(new ChangePasswordCommand(Guid.NewGuid(), "OldPass1", "NewPass1"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyCurrentPassword_HasErrors()
    {
        var result = _validator.Validate(new ChangePasswordCommand(Guid.NewGuid(), "", "NewPass1"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NewPasswordSameAsCurrent_HasErrors()
    {
        var result = _validator.Validate(new ChangePasswordCommand(Guid.NewGuid(), "SamePass1", "SamePass1"));

        Assert.False(result.IsValid);
    }
}
