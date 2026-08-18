using FluentValidation;

namespace Application.Authentication.Commands
{
    public sealed class RegenerateRecoveryCodesCommandValidator : AbstractValidator<RegenerateRecoveryCodesCommand>
    {
        public RegenerateRecoveryCodesCommandValidator()
        {
            RuleFor(x => x.CurrentPassword).NotEmpty();
        }
    }
}
