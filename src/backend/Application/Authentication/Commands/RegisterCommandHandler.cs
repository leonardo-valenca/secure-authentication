using Application.Abstractions.Notifications;
using Application.Abstractions.Persistence;
using Application.Authentication.Responses;
using Domain.Common;
using Domain.Users;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Application.Authentication.Commands
{
    public sealed class RegisterCommandHandler(
        IUserRepository userRepository,
        IEmailSender emailSender,
        ILogger<RegisterCommandHandler> logger) : IRequestHandler<RegisterCommand, Result<AuthenticationResponse>>
    {
        public async ValueTask<Result<AuthenticationResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var emailResult = Email.Create(request.Email);
            if (emailResult.IsFailure)
                return Result.Failure<AuthenticationResponse>(emailResult.Error);

            if (await userRepository.ExistsByEmailAsync(emailResult.Value, cancellationToken))
                return Result.Failure<AuthenticationResponse>(UserErrors.EmailAlreadyInUse);

            var createResult = await userRepository.CreateAsync(emailResult.Value, request.Password, cancellationToken);
            if (createResult.IsFailure)
                return Result.Failure<AuthenticationResponse>(createResult.Error);

            var user = createResult.Value;

            // The account was just created, so a token always exists here, null is only possible
            // for an email that isn't registered, which this one now is.
            var confirmationToken = await userRepository.GenerateEmailConfirmationTokenAsync(emailResult.Value, cancellationToken);

            try
            {
                await emailSender.SendEmailConfirmationEmailAsync(user.Email.Value, confirmationToken!, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // The account already exists at this point, unlike forgot-password (where a
                // failed send is safe to just retry), letting this fail the whole request would
                // leave the user stuck: retrying registration fails with EmailAlreadyInUse, and
                // they'd have no reason to know the resend-confirmation endpoint exists. Log it
                // and let them recover via resend instead of turning a delivery hiccup (SMTP
                // outage, bad credentials, ...) into a registration failure.
                logger.LogWarning(exception, "Failed to send confirmation email to {Email} after account creation", user.Email.Value);
            }

            logger.UserRegistered(user.Id, user.Email.Value);

            return new AuthenticationResponse(user.Id, user.Email.Value);
        }
    }
}