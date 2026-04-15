using otelturizmnew.Models.Messages;

namespace otelturizmnew.Services.Abstractions;

public interface ISecureFileService
{
    Task<StoredSecureFileResult> SaveAsync(IFormFile file, SecureFileSaveRequest request, CancellationToken cancellationToken = default);
    Task<string> CreateAccessUrlAsync(long fileId, long viewerUserId, string viewerAccountType, CancellationToken cancellationToken = default);
    Task<SecureFileDownloadResult?> ResolveDownloadAsync(string token, long viewerUserId, string viewerAccountType, CancellationToken cancellationToken = default);
}
