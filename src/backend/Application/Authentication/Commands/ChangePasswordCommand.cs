using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest<Result>;
}
