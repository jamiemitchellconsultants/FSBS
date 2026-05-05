namespace FSBS.Infrastructure.Email;

public sealed class SesSettings
{
    /// <summary>Verified SES sender address (e.g. noreply@fsbs.example.com).</summary>
    public string FromAddress { get; set; } = string.Empty;
}
