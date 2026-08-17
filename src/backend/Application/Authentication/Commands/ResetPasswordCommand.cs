using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<Result>;
}
