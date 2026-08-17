using Application.Abstractions.Persistence;
using Application.Abstractions.Security;
using Domain.Common;
using Domain.Users;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Application.Authentication.Commands
{
    public sealed class LoginCommandHandler(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        ITokenHasher tokenHasher,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        ILogger<LoginCommandHandler> logger) : IRequestHandler<LoginCommand, Result<LoginOutcome>>
    {
        public async ValueTask<Result<LoginOutcome>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var emailResult = Email.Create(request.Email);
            if (emailResult.IsFailure)
                return Result.Failure<LoginOutcome>(UserErrors.InvalidCredentials);

            var verifyResult = await userRepository.VerifyCredentialsAsync(emailResult.Value, request.Password, cancellationToken);
            if (verifyResult.IsFailure)
                return Result.Failure<LoginOutcome>(verifyResult.Error);

            var (user, requiresTwoFactor) = verifyResult.Value;

            if (requiresTwoFactor)
                return new LoginOutcome.RequiresTwoFactor(user.Id);

            var loginResult = await LoginTokenIssuer.IssueAsync(user, jwtTokenGenerator, tokenHasher, refreshTokenRepository, unitOfWork, cancellationToken);

            logger.UserLoggedIn(user.Id);

            return new LoginOutcome.Completed(loginResult);
        }
    }
}
