using Application.Authentication.Commands;

namespace Application.Tests.Authentication.Commands;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(new RegisterCommand("user@example.com", "StrongPass1"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyPassword_HasErrors()
    {
        var result = _validator.Validate(new RegisterCommand("user@example.com", ""));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_PasswordOverMaximumLength_HasErrors()
    {
        var result = _validator.Validate(new RegisterCommand("user@example.com", new string('a', 129)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_InvalidEmail_HasErrors()
    {
        var result = _validator.Validate(new RegisterCommand("not-an-email", "StrongPass1"));

        Assert.False(result.IsValid);
    }
}
