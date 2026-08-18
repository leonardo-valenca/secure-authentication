using Application.Abstractions.Persistence;
using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed class DeleteAccountCommandHandler(IUserRepository userRepository)
        : IRequestHandler<DeleteAccountCommand, Result>
    {
        public async ValueTask<Result> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
        {
            return await userRepository.DeleteAccountAsync(request.UserId, request.CurrentPassword, cancellationToken);
        }
    }
}
