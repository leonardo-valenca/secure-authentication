using Application.Abstractions.Persistence;
using Application.Abstractions.Security;
using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed class CompleteTwoFactorLoginCommandHandler(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        ITokenHasher tokenHasher,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<CompleteTwoFactorLoginCommand, Result<LoginResult>>
    {
        public async ValueTask<Result<LoginResult>> Handle(CompleteTwoFactorLoginCommand request, CancellationToken cancellationToken)
        {
            var verifyResult = await userRepository.VerifyTwoFactorCodeAsync(request.UserId, request.Code, cancellationToken);
            if (verifyResult.IsFailure)
                return Result.Failure<LoginResult>(verifyResult.Error);

            var loginResult = await LoginTokenIssuer.IssueAsync(verifyResult.Value, jwtTokenGenerator, tokenHasher, refreshTokenRepository, unitOfWork, cancellationToken);
            return loginResult;
        }
    }
}
