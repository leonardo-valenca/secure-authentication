using FluentValidation;

namespace Application.Authentication.Commands
{
    public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            // Complexity (length, digit/case requirements) is enforced by Identity's PasswordOptions
            // instead, so it applies uniformly to register/reset/change. See Infrastructure.DependencyInjection.
            RuleFor(x => x.Password)
                .NotEmpty()
                // Caps the cost of PBKDF2 hashing an attacker-supplied string. An unbounded
                // password length is a cheap way to burn CPU on every registration attempt.
                .MaximumLength(128);
        }
    }
}
