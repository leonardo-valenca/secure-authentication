using Application.Authentication.Commands;

namespace Application.Tests.Authentication.Commands;

public class EnableTwoFactorCommandValidatorTests
{
    private readonly EnableTwoFactorCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var result = _validator.Validate(new EnableTwoFactorCommand(Guid.NewGuid(), "123456"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyCode_HasErrors()
    {
        var result = _validator.Validate(new EnableTwoFactorCommand(Guid.NewGuid(), ""));

        Assert.False(result.IsValid);
    }
}
