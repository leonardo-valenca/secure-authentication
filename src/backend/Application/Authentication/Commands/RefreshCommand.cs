using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed record RefreshCommand(string RefreshToken) : IRequest<Result<LoginResult>>;
}
