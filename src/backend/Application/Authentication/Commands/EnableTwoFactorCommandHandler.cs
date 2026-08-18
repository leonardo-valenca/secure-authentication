using Application.Abstractions.Persistence;
using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed class EnableTwoFactorCommandHandler(IUserRepository userRepository)
        : IRequestHandler<EnableTwoFactorCommand, Result<IReadOnlyList<string>>>
    {
        public async ValueTask<Result<IReadOnlyList<string>>> Handle(EnableTwoFactorCommand request, CancellationToken cancellationToken)
        {
            return await userRepository.EnableTwoFactorAsync(request.UserId, request.Code, cancellationToken);
        }
    }
}
