using FluentValidation;

namespace Application.Authentication.Commands
{
    public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.CurrentPassword).NotEmpty();

            // Complexity is enforced by Identity's PasswordOptions, see Infrastructure.DependencyInjection.
            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .MaximumLength(128)
                .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from the current password.");
        }
    }
}
