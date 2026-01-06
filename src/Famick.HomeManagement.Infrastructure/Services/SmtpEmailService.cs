using System.Net;
using System.Net.Mail;
using Famick.HomeManagement.Core.Configuration;
using Famick.HomeManagement.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Famick.HomeManagement.Infrastructure.Services;

/// <summary>
/// SMTP-based email service implementation
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<EmailSettings> settings,
        ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendPasswordResetEmailAsync(
        string toEmail,
        string userName,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        var subject = "Reset Your Password - Famick Home Management";
        var htmlBody = GeneratePasswordResetEmailHtml(userName, resetLink);
        var textBody = GeneratePasswordResetEmailText(userName, resetLink);

        await SendEmailAsync(toEmail, subject, htmlBody, textBody, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendPasswordResetConfirmationEmailAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var subject = "Password Changed - Famick Home Management";
        var htmlBody = GeneratePasswordChangedEmailHtml(userName);
        var textBody = GeneratePasswordChangedEmailText(userName);

        await SendEmailAsync(toEmail, subject, htmlBody, textBody, cancellationToken);
    }

    private async Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_settings.SmtpHost))
        {
            _logger.LogWarning("SMTP host not configured. Email to {Email} not sent.", toEmail);
            return;
        }

        try
        {
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = !string.IsNullOrEmpty(_settings.SmtpUsername)
                    ? new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword)
                    : null
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                IsBodyHtml = true,
                Body = htmlBody
            };

            message.To.Add(toEmail);

            // Add plain text alternative
            var plainTextView = AlternateView.CreateAlternateViewFromString(
                textBody, null, "text/plain");
            var htmlView = AlternateView.CreateAlternateViewFromString(
                htmlBody, null, "text/html");

            message.AlternateViews.Add(plainTextView);
            message.AlternateViews.Add(htmlView);

            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }

    private static string GeneratePasswordResetEmailHtml(string userName, string resetLink)
    {
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
                    .container { max-width: 600px; margin: 0 auto; padding: 20px; }
                    .button { display: inline-block; padding: 12px 24px; background-color: #1976D2;
                               color: white; text-decoration: none; border-radius: 4px; margin: 20px 0; }
                    .footer { margin-top: 30px; font-size: 12px; color: #666; }
                </style>
            </head>
            <body>
                <div class="container">
                    <h2>Reset Your Password</h2>
                    <p>Hi {{userName}},</p>
                    <p>We received a request to reset your password for your Famick Home Management account.</p>
                    <p>Click the button below to reset your password:</p>
                    <a href="{{resetLink}}" class="button">Reset Password</a>
                    <p>This link will expire in 15 minutes.</p>
                    <p>If you didn't request a password reset, you can safely ignore this email.
                       Your password will remain unchanged.</p>
                    <div class="footer">
                        <p>If the button doesn't work, copy and paste this link into your browser:</p>
                        <p>{{resetLink}}</p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }

    private static string GeneratePasswordResetEmailText(string userName, string resetLink)
    {
        return $"""
            Reset Your Password

            Hi {userName},

            We received a request to reset your password for your Famick Home Management account.

            Click the link below to reset your password:
            {resetLink}

            This link will expire in 15 minutes.

            If you didn't request a password reset, you can safely ignore this email.
            Your password will remain unchanged.
            """;
    }

    private static string GeneratePasswordChangedEmailHtml(string userName)
    {
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
                    .container { max-width: 600px; margin: 0 auto; padding: 20px; }
                </style>
            </head>
            <body>
                <div class="container">
                    <h2>Password Changed Successfully</h2>
                    <p>Hi {{userName}},</p>
                    <p>Your password for Famick Home Management has been successfully changed.</p>
                    <p>If you did not make this change, please contact support immediately.</p>
                </div>
            </body>
            </html>
            """;
    }

    private static string GeneratePasswordChangedEmailText(string userName)
    {
        return $"""
            Password Changed Successfully

            Hi {userName},

            Your password for Famick Home Management has been successfully changed.

            If you did not make this change, please contact support immediately.
            """;
    }

    /// <inheritdoc />
    public async Task SendWelcomeEmailAsync(
        string toEmail,
        string userName,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        var subject = "Welcome to Famick Home Management";
        var htmlBody = GenerateWelcomeEmailHtml(userName, toEmail, temporaryPassword);
        var textBody = GenerateWelcomeEmailText(userName, toEmail, temporaryPassword);

        await SendEmailAsync(toEmail, subject, htmlBody, textBody, cancellationToken);
    }

    private static string GenerateWelcomeEmailHtml(string userName, string email, string temporaryPassword)
    {
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
                    .container { max-width: 600px; margin: 0 auto; padding: 20px; }
                    .credentials { background-color: #f5f5f5; padding: 15px; border-radius: 4px; margin: 20px 0; }
                    .credentials p { margin: 5px 0; }
                    .warning { color: #d32f2f; font-weight: bold; }
                </style>
            </head>
            <body>
                <div class="container">
                    <h2>Welcome to Famick Home Management</h2>
                    <p>Hi {{userName}},</p>
                    <p>An account has been created for you. Here are your login credentials:</p>
                    <div class="credentials">
                        <p><strong>Email:</strong> {{email}}</p>
                        <p><strong>Temporary Password:</strong> {{temporaryPassword}}</p>
                    </div>
                    <p class="warning">Please change your password after your first login.</p>
                    <p>If you did not expect this email, please contact your administrator.</p>
                </div>
            </body>
            </html>
            """;
    }

    private static string GenerateWelcomeEmailText(string userName, string email, string temporaryPassword)
    {
        return $"""
            Welcome to Famick Home Management

            Hi {userName},

            An account has been created for you. Here are your login credentials:

            Email: {email}
            Temporary Password: {temporaryPassword}

            Please change your password after your first login.

            If you did not expect this email, please contact your administrator.
            """;
    }
}
