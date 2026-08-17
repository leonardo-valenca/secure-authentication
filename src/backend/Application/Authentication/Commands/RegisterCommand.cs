using Application.Authentication.Responses;
using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed record RegisterCommand(string Email, string Password) : IRequest<Result<AuthenticationResponse>>;
}
