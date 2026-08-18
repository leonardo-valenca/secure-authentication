using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed record DisableTwoFactorCommand(Guid UserId, string CurrentPassword) : IRequest<Result>;
}
