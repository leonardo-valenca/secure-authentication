using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed record ResendConfirmationEmailCommand(string Email) : IRequest<Result>;
}
