using Application.Abstractions.Persistence;
using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed class GetTwoFactorStatusCommandHandler(IUserRepository userRepository)
        : IRequestHandler<GetTwoFactorStatusCommand, Result<bool>>
    {
        public async ValueTask<Result<bool>> Handle(GetTwoFactorStatusCommand request, CancellationToken cancellationToken)
        {
            return await userRepository.GetTwoFactorStatusAsync(request.UserId, cancellationToken);
        }
    }
}
