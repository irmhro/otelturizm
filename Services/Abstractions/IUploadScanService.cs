using otelturizmnew.Models.Messages;

namespace otelturizmnew.Services.Abstractions;

public interface IUploadScanService
{
    Task ScanOrThrowAsync(string absolutePath, StoredSecureFileResult storedFile, CancellationToken cancellationToken = default);
}

