namespace otelturizmnew.Services.Abstractions;

public interface IUploadAuditService
{
    Task RecordAsync(UploadAuditEvent evt, CancellationToken cancellationToken = default);
}

public sealed record UploadAuditEvent(
    string Kind,
    string Category,
    long SizeBytes,
    string StoredName,
    string StoredPathOrUrl,
    string ContentType,
    string Extension,
    string Sha256,
    long? OwnerUserId,
    long? OwnerFirmaId,
    string? ContextTable,
    long? ContextId,
    string? RemoteIp,
    string? UserAgent);

