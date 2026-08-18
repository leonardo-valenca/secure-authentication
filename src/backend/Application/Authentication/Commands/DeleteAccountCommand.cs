using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed record DeleteAccountCommand(Guid UserId, string CurrentPassword) : IRequest<Result>;
}
