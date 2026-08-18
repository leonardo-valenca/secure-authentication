using FluentValidation;

namespace Application.Authentication.Commands
{
    public sealed class DisableTwoFactorCommandValidator : AbstractValidator<DisableTwoFactorCommand>
    {
        public DisableTwoFactorCommandValidator()
        {
            RuleFor(x => x.CurrentPassword).NotEmpty();
        }
    }
}
