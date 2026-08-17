using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;
}
