namespace FSBS.Application.Common.Interfaces;

/// <summary>
/// Sends transactional emails via Amazon SES using pre-registered templates.
/// </summary>
public interface ISesEmailService
{
    /// <summary>
    /// Sends a templated email to a single recipient.
    /// </summary>
    /// <param name="toAddress">Recipient email address.</param>
    /// <param name="templateName">SES template name registered in the account.</param>
    /// <param name="templateData">Object serialised to JSON and merged into the template.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendTemplatedEmailAsync(
        string toAddress,
        string templateName,
        object templateData,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a plain-text / HTML email without a pre-registered template.
    /// Used for ad-hoc operational messages.
    /// </summary>
    Task SendEmailAsync(
        string toAddress,
        string subject,
        string htmlBody,
        CancellationToken ct = default);
}
