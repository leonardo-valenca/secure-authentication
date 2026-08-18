using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed record GetTwoFactorStatusCommand(Guid UserId) : IRequest<Result<bool>>;
}
