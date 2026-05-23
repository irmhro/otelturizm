using Microsoft.Extensions.Logging;
using otelturizmnew.Services.Abstractions;
using otelturizmnew.Utils;

namespace otelturizmnew.Services;

public sealed class UploadAuditService : IUploadAuditService
{
    private readonly ILogger<UploadAuditService> _logger;

    public UploadAuditService(ILogger<UploadAuditService> logger)
    {
        _logger = logger;
    }

    public Task RecordAsync(UploadAuditEvent evt, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "UPLOAD_AUDIT kind={Kind} category={Category} size={SizeBytes} ext={Ext} ct={ContentType} sha={Sha256} stored={StoredName} path={StoredPath} ownerUser={OwnerUserId} ownerFirma={OwnerFirmaId} ctx={ContextTable}:{ContextId} ip={Ip} ua={Ua}",
            evt.Kind,
            evt.Category,
            evt.SizeBytes,
            evt.Extension,
            evt.ContentType,
            evt.Sha256,
            evt.StoredName,
            LogRedaction.RedactStoredPath(evt.StoredPathOrUrl),
            evt.OwnerUserId,
            evt.OwnerFirmaId,
            evt.ContextTable ?? string.Empty,
            evt.ContextId,
            LogRedaction.MaskIp(evt.RemoteIp),
            LogRedaction.TruncateUserAgent(evt.UserAgent));

        return Task.CompletedTask;
    }
}

