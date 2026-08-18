using FluentValidation;

namespace Application.Authentication.Commands
{
    public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Token).NotEmpty();

            // Complexity is enforced by Identity's PasswordOptions, see Infrastructure.DependencyInjection.
            RuleFor(x => x.NewPassword).NotEmpty().MaximumLength(128);
        }
    }
}
