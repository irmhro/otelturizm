using otelturizmnew.Models.Messages;

namespace otelturizmnew.Services.Abstractions;

public interface ISecureFileService
{
    Task<StoredSecureFileResult> SaveAsync(IFormFile file, SecureFileSaveRequest request, CancellationToken cancellationToken = default);
    Task<string> CreateAccessUrlAsync(long fileId, long viewerUserId, string viewerAccountType, CancellationToken cancellationToken = default);
    /// <summary>E-posta linkleri icin uzun omurlu (7 gun) erisim tokeni.</summary>
    Task<string> CreateEmailAccessUrlAsync(long fileId, long viewerUserId, string viewerAccountType, CancellationToken cancellationToken = default);
    Task<SecureFileDownloadResult?> ResolveDownloadAsync(string token, long viewerUserId, string viewerAccountType, CancellationToken cancellationToken = default);
    Task<bool> DeleteOwnedFileAsync(long fileId, long ownerUserId, string category, CancellationToken cancellationToken = default);
}
