using Application.Abstractions.Persistence;
using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed class DisableTwoFactorCommandHandler(IUserRepository userRepository)
        : IRequestHandler<DisableTwoFactorCommand, Result>
    {
        public async ValueTask<Result> Handle(DisableTwoFactorCommand request, CancellationToken cancellationToken)
        {
            return await userRepository.DisableTwoFactorAsync(request.UserId, request.CurrentPassword, cancellationToken);
        }
    }
}
