using FluentValidation;

namespace Application.Authentication.Commands
{
    public sealed class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
    {
        public DeleteAccountCommandValidator()
        {
            RuleFor(x => x.CurrentPassword).NotEmpty();
        }
    }
}
