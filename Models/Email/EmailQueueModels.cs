namespace otelturizmnew.Models.Email;

public class QueuedEmailTemplateRequest
{
    public long UserId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string? ServiceCodeOverride { get; set; }
    public string? SenderEmailOverride { get; set; }
    public Dictionary<string, string> Tokens { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>ILGILI_TABLO — örn. KULLANICILAR, REZERVASYONLAR.</summary>
    public string? RelatedTable { get; set; }
    public long? RelatedRecordId { get; set; }
    public string? SubjectOverride { get; set; }
    public List<QueuedEmailAttachment>? Attachments { get; set; }
}

public class QueuedEmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public string FilePathOrUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
}

public class EmailProviderSettings
{
    public string Provider { get; set; } = "SMTP";
    public string ServiceCode { get; set; } = "default_smtp";
    public string SenderName { get; set; } = "Otelturizm";
    public string SenderEmail { get; set; } = "no-reply@otelturizm.com";
    public bool TestMode { get; set; } = true;
}
