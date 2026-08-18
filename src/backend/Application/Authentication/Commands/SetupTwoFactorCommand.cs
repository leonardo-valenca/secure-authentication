using Application.Authentication.Responses;
using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed record SetupTwoFactorCommand(Guid UserId) : IRequest<Result<TwoFactorSetup>>;
}
