using Microsoft.AspNetCore.Http;

namespace otelturizmnew.Services.Abstractions;

public interface IImageStorageService
{
    Task<ImageStorageResult> SaveAsWebpAsync(IFormFile file, string targetDirectory, string filePrefix, CancellationToken cancellationToken = default);
    Task<ImageStorageResult> SaveAsWebpAsync(IFormFile file, ImageSaveRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string physicalPath, CancellationToken cancellationToken = default);
}

public sealed class ImageStorageResult
{
    public string FileName { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public string? Sha256 { get; init; }
    public IReadOnlyList<ImageVariant> Variants { get; init; } = Array.Empty<ImageVariant>();
}

public sealed record ImageVariant(string Label, string FileName, int Width, int Height, long SizeBytes);

public sealed record ImageSaveRequest(
    string TargetDirectory,
    string FilePrefix,
    string Category,
    long? OwnerUserId = null,
    long? OwnerFirmaId = null,
    string? ContextTable = null,
    long? ContextId = null,
    ImageQualityProfile QualityProfile = ImageQualityProfile.Default,
    bool GenerateThumbnails = true);

public enum ImageQualityProfile
{
    Default = 0,
    Avatar = 1,
    HotelPhoto = 2,
    RoomPhoto = 3,
    RequestVisual = 4
}
