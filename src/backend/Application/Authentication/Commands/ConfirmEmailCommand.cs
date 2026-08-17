using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed record ConfirmEmailCommand(string Email, string Token) : IRequest<Result>;
}
