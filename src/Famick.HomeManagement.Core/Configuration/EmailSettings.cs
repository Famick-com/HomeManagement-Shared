namespace Famick.HomeManagement.Core.Configuration;

/// <summary>
/// SMTP email configuration settings
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "EmailSettings";

    /// <summary>
    /// SMTP server host
    /// </summary>
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port (typically 587 for TLS, 465 for SSL)
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// SMTP username for authentication
    /// </summary>
    public string SmtpUsername { get; set; } = string.Empty;

    /// <summary>
    /// SMTP password for authentication
    /// </summary>
    public string SmtpPassword { get; set; } = string.Empty;

    /// <summary>
    /// Enable SSL/TLS for SMTP connection
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// From email address
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// From display name
    /// </summary>
    public string FromName { get; set; } = "Famick Home Management";

    /// <summary>
    /// Password reset token expiration in minutes
    /// </summary>
    public int PasswordResetTokenExpirationMinutes { get; set; } = 15;
}
