using Application.Abstractions.Persistence;
using Application.Authentication.Responses;
using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed class SetupTwoFactorCommandHandler(IUserRepository userRepository)
        : IRequestHandler<SetupTwoFactorCommand, Result<TwoFactorSetup>>
    {
        public async ValueTask<Result<TwoFactorSetup>> Handle(SetupTwoFactorCommand request, CancellationToken cancellationToken)
        {
            return await userRepository.GenerateTwoFactorSetupAsync(request.UserId, cancellationToken);
        }
    }
}
