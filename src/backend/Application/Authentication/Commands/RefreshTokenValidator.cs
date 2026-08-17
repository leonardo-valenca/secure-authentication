using FluentValidation;

namespace Application.Authentication.Commands
{
    public sealed class RefreshCommandValidator : AbstractValidator<RefreshCommand>
    {
        public RefreshCommandValidator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty();
        }
    }
}
