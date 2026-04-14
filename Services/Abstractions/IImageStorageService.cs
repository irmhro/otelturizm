using Microsoft.AspNetCore.Http;

namespace otelturizmnew.Services.Abstractions;

public interface IImageStorageService
{
    Task<ImageStorageResult> SaveAsWebpAsync(IFormFile file, string targetDirectory, string filePrefix, CancellationToken cancellationToken = default);
    Task DeleteAsync(string physicalPath, CancellationToken cancellationToken = default);
}

public sealed class ImageStorageResult
{
    public string FileName { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
}
