using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

namespace FSBS.Worker.Email;

/// <summary>
/// Ensures all required SES email templates exist in the AWS account.
/// Called once at worker startup via <see cref="IHostedService"/>.
/// If a template already exists it is updated (upsert behaviour).
/// </summary>
internal sealed class SesTemplateSeeder(
    IAmazonSimpleEmailService ses,
    ILogger<SesTemplateSeeder> logger) : IHostedService
{
    private static readonly IReadOnlyList<Template> Templates =
    [
        new Template
        {
            TemplateName = "FsbsBookingConfirmed",
            SubjectPart  = "Your FSBS booking is confirmed — {{bookingId}}",
            HtmlPart     = """
                <h2>Booking Confirmed</h2>
                <p>Hi {{name}},</p>
                <p>Your simulator booking <strong>{{bookingId}}</strong> has been confirmed.</p>
                <ul>
                  <li>Gross price: £{{grossPrice}}</li>
                  <li>Discount: £{{discount}}</li>
                  <li>Net price: £{{netPrice}}</li>
                </ul>
                <p>Confirmed at: {{occurredAt}}</p>
                """,
            TextPart = "Hi {{name}}, your booking {{bookingId}} is confirmed. Net price: £{{netPrice}}."
        },
        new Template
        {
            TemplateName = "FsbsBookingPendingApproval",
            SubjectPart  = "FSBS: New booking awaiting approval — {{bookingId}}",
            HtmlPart     = """
                <h2>Booking Pending Approval</h2>
                <p>A new InternalStudent booking requires your approval.</p>
                <ul>
                  <li>Booking ID: {{bookingId}}</li>
                  <li>Booker: {{bookerName}}</li>
                  <li>Training type: {{trainingType}}</li>
                  <li>Slot: {{slotStart}} – {{slotEnd}}</li>
                  <li>Students: {{studentCount}}</li>
                </ul>
                <p>Please review in the FSBS staff portal.</p>
                """,
            TextPart = "New booking {{bookingId}} by {{bookerName}} awaiting approval. Slot: {{slotStart}} - {{slotEnd}}."
        },
        new Template
        {
            TemplateName = "FsbsBookingApproved",
            SubjectPart  = "Your FSBS booking has been approved — {{bookingId}}",
            HtmlPart     = """
                <h2>Booking Approved</h2>
                <p>Hi {{name}},</p>
                <p>Your booking <strong>{{bookingId}}</strong> has been approved.</p>
                <p>Approved at: {{approvedAt}}</p>
                """,
            TextPart = "Hi {{name}}, your booking {{bookingId}} has been approved."
        },
        new Template
        {
            TemplateName = "FsbsBookingRejected",
            SubjectPart  = "Your FSBS booking has been rejected — {{bookingId}}",
            HtmlPart     = """
                <h2>Booking Rejected</h2>
                <p>Hi {{name}},</p>
                <p>Unfortunately your booking <strong>{{bookingId}}</strong> has been rejected.</p>
                <p><strong>Reason:</strong> {{rejectionReason}}</p>
                <p>Rejected at: {{rejectedAt}}</p>
                <p>Please contact SalesStaff if you have any questions.</p>
                """,
            TextPart = "Hi {{name}}, your booking {{bookingId}} was rejected. Reason: {{rejectionReason}}."
        },
        new Template
        {
            TemplateName = "FsbsBookingCancelled",
            SubjectPart  = "Your FSBS booking has been cancelled — {{bookingId}}",
            HtmlPart     = """
                <h2>Booking Cancelled</h2>
                <p>Hi {{name}},</p>
                <p>Your booking <strong>{{bookingId}}</strong> has been cancelled.</p>
                {{#cancelledByAdmin}}<p>This cancellation was made by an administrator.</p>{{/cancelledByAdmin}}
                {{#reason}}<p><strong>Reason:</strong> {{reason}}</p>{{/reason}}
                <p>Cancelled at: {{cancelledAt}}</p>
                """,
            TextPart = "Hi {{name}}, your booking {{bookingId}} has been cancelled."
        },
        new Template
        {
            TemplateName = "FsbsInvitationIssued",
            SubjectPart  = "You've been invited to FSBS — {{organisationName}}",
            HtmlPart     = """
                <h2>You've been invited to FSBS</h2>
                <p>Hi,</p>
                <p>You have been invited to register as a <strong>{{role}}</strong>
                for <strong>{{organisationName}}</strong> on the FSBS booking platform.</p>
                <p>Click the link below to complete your registration. The link expires on
                <strong>{{expiresAt}}</strong>.</p>
                <p><a href="https://app.fsbs.example.com/register?token={{token}}&email={{email}}">
                  Complete your registration
                </a></p>
                <p>If you did not expect this invitation you can safely ignore this email.</p>
                """,
            TextPart = "You've been invited to FSBS as a {{role}} for {{organisationName}}. " +
                       "Register at https://app.fsbs.example.com/register?token={{token}}&email={{email}}. " +
                       "Link expires {{expiresAt}}."
        }
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var template in Templates)
        {
            await UpsertTemplateAsync(template, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task UpsertTemplateAsync(Template template, CancellationToken ct)
    {
        try
        {
            await ses.UpdateTemplateAsync(new UpdateTemplateRequest { Template = template }, ct);
            logger.LogDebug("SES template '{TemplateName}' updated.", template.TemplateName);
        }
        catch (TemplateDoesNotExistException)
        {
            await ses.CreateTemplateAsync(new CreateTemplateRequest { Template = template }, ct);
            logger.LogInformation("SES template '{TemplateName}' created.", template.TemplateName);
        }
    }
}
