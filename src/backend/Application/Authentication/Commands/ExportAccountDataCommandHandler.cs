using Application.Abstractions.Persistence;
using Application.Authentication.Responses;
using Domain.Common;
using Domain.Users;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed class ExportAccountDataCommandHandler(IUserRepository userRepository)
        : IRequestHandler<ExportAccountDataCommand, Result<AccountDataExport>>
    {
        public async ValueTask<Result<AccountDataExport>> Handle(ExportAccountDataCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null)
                return Result.Failure<AccountDataExport>(UserErrors.AccountNotFound);

            return new AccountDataExport(user.Id, user.Email.Value, user.CreatedAtUtc);
        }
    }
}
