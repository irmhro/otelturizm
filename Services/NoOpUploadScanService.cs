using otelturizmnew.Models.Messages;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class NoOpUploadScanService : IUploadScanService
{
    public Task ScanOrThrowAsync(string absolutePath, StoredSecureFileResult storedFile, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

