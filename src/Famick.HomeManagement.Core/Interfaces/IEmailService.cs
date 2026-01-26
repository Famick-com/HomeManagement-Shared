namespace Famick.HomeManagement.Core.Interfaces;

/// <summary>
/// Service for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email verification link for new user registration
    /// </summary>
    /// <param name="toEmail">The recipient's email address</param>
    /// <param name="householdName">The household name being registered</param>
    /// <param name="verificationLink">The email verification link (deep link for mobile)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendEmailVerificationAsync(
        string toEmail,
        string householdName,
        string verificationLink,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset email to the user
    /// </summary>
    /// <param name="toEmail">The recipient's email address</param>
    /// <param name="userName">The user's display name</param>
    /// <param name="resetLink">The password reset link</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendPasswordResetEmailAsync(
        string toEmail,
        string userName,
        string resetLink,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset confirmation email after successful reset
    /// </summary>
    /// <param name="toEmail">The recipient's email address</param>
    /// <param name="userName">The user's display name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendPasswordResetConfirmationEmailAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a welcome email to a newly created user with their login credentials
    /// </summary>
    /// <param name="toEmail">The recipient's email address</param>
    /// <param name="userName">The user's display name</param>
    /// <param name="temporaryPassword">The temporary password for initial login</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendWelcomeEmailAsync(
        string toEmail,
        string userName,
        string temporaryPassword,
        CancellationToken cancellationToken = default);
}
