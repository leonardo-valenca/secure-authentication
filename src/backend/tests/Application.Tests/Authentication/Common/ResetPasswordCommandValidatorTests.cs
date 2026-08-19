using Application.Authentication.Commands;

namespace Application.Tests.Authentication.Commands;

public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var result = _validator.Validate(new ResetPasswordCommand("user@example.com", "token", "NewPass1"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyToken_HasErrors()
    {
        var result = _validator.Validate(new ResetPasswordCommand("user@example.com", "", "NewPass1"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_PasswordOverMaximumLength_HasErrors()
    {
        var result = _validator.Validate(new ResetPasswordCommand("user@example.com", "token", new string('a', 129)));

        Assert.False(result.IsValid);
    }
}
