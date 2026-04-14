using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class ImageStorageService : IImageStorageService
{
    private const long MaxUploadBytes = 15 * 1024 * 1024;
    private const int MaxDimension = 2560;
    private const int WebpQuality = 90;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".tif", ".tiff"
    };

    public async Task<ImageStorageResult> SaveAsWebpAsync(IFormFile file, string targetDirectory, string filePrefix, CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("Bos dosya yuklenemez.");
        }

        if (file.Length > MaxUploadBytes)
        {
            throw new InvalidOperationException("Tek bir gorsel en fazla 15 MB olabilir.");
        }

        var extension = Path.GetExtension(file.FileName);
        var hasAllowedExtension = !string.IsNullOrWhiteSpace(extension) && AllowedExtensions.Contains(extension);
        var hasImageContentType = !string.IsNullOrWhiteSpace(file.ContentType)
            && file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

        if (!hasImageContentType && !hasAllowedExtension)
        {
            throw new InvalidOperationException("Yalnizca gorsel dosyalari yuklenebilir.");
        }

        Directory.CreateDirectory(targetDirectory);

        await using var inputStream = file.OpenReadStream();
        using var image = await Image.LoadAsync(inputStream, cancellationToken);
        image.Mutate(context =>
        {
            context.AutoOrient();

            if (image.Width > MaxDimension || image.Height > MaxDimension)
            {
                context.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(MaxDimension, MaxDimension)
                });
            }
        });

        var fileName = $"{filePrefix}-{Guid.NewGuid():N}.webp";
        var physicalPath = Path.Combine(targetDirectory, fileName);
        var encoder = new WebpEncoder
        {
            FileFormat = WebpFileFormatType.Lossy,
            Quality = WebpQuality
        };

        await using var outputStream = File.Create(physicalPath);
        await image.SaveAsync(outputStream, encoder, cancellationToken);
        await outputStream.FlushAsync(cancellationToken);

        var fileInfo = new FileInfo(physicalPath);
        return new ImageStorageResult
        {
            FileName = fileName,
            FileSizeBytes = fileInfo.Length,
            Width = image.Width,
            Height = image.Height
        };
    }

    public Task DeleteAsync(string physicalPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(physicalPath))
        {
            return Task.CompletedTask;
        }

        if (!File.Exists(physicalPath))
        {
            return Task.CompletedTask;
        }

        File.Delete(physicalPath);

        var directory = Path.GetDirectoryName(physicalPath);
        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
        {
            Directory.Delete(directory, false);
        }

        return Task.CompletedTask;
    }
}
