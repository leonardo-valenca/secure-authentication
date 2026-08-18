using Application.Abstractions.Persistence;
using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed class RegenerateRecoveryCodesCommandHandler(IUserRepository userRepository)
        : IRequestHandler<RegenerateRecoveryCodesCommand, Result<IReadOnlyList<string>>>
    {
        public async ValueTask<Result<IReadOnlyList<string>>> Handle(RegenerateRecoveryCodesCommand request, CancellationToken cancellationToken)
        {
            return await userRepository.RegenerateRecoveryCodesAsync(request.UserId, request.CurrentPassword, cancellationToken);
        }
    }
}
