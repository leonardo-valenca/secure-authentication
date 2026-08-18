using FluentValidation;

namespace Application.Authentication.Commands
{
    public sealed class EnableTwoFactorCommandValidator : AbstractValidator<EnableTwoFactorCommand>
    {
        public EnableTwoFactorCommandValidator()
        {
            RuleFor(x => x.Code).NotEmpty();
        }
    }
}
