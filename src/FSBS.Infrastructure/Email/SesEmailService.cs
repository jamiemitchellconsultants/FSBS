using System.Text.Json;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using FSBS.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace FSBS.Infrastructure.Email;

/// <summary>
/// Amazon SES implementation of <see cref="ISesEmailService"/>.
/// </summary>
internal sealed class SesEmailService(
    IAmazonSimpleEmailService ses,
    IOptions<SesSettings> options) : ISesEmailService
{
    private readonly SesSettings _settings = options.Value;

    /// <summary>Sends a templated SES email using a pre-registered template name and JSON data object.</summary>
    public async Task SendTemplatedEmailAsync(
        string toAddress,
        string templateName,
        object templateData,
        CancellationToken ct = default)
    {
        var request = new SendTemplatedEmailRequest
        {
            Source      = _settings.FromAddress,
            Destination = new Destination { ToAddresses = [toAddress] },
            Template    = templateName,
            TemplateData = JsonSerializer.Serialize(templateData)
        };
        await ses.SendTemplatedEmailAsync(request, ct);
    }

    /// <summary>Sends a plain HTML email with an explicit subject and body.</summary>
    public async Task SendEmailAsync(
        string toAddress,
        string subject,
        string htmlBody,
        CancellationToken ct = default)
    {
        var request = new SendEmailRequest
        {
            Source      = _settings.FromAddress,
            Destination = new Destination { ToAddresses = [toAddress] },
            Message     = new Message
            {
                Subject = new Content(subject),
                Body    = new Body { Html = new Content(htmlBody) }
            }
        };
        await ses.SendEmailAsync(request, ct);
    }
}
