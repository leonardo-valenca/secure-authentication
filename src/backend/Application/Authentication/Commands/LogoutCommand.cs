using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed record LogoutCommand(string RefreshToken) : IRequest<Result>;
}
