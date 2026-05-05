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
