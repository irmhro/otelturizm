using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using otelturizmnew.Services.Abstractions;
using System.Security.Cryptography;
using otelturizmnew.Utils;

namespace otelturizmnew.Services;

public class ImageStorageService : IImageStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ImageStorageService> _logger;
    private readonly IUploadAuditService _uploadAuditService;

    // T303: upload limits (input bytes before WebP). Partner otel/oda: HotelPhoto/RoomPhoto 15 MB (~15360 KB).
    private const long DefaultMaxUploadBytes = 15 * 1024 * 1024; // 15360 KB
    private const int DefaultMaxDimension = 2560;
    private const int DefaultWebpQuality = 90;
    private const long MaxPixelCount = 35_000_000; // decompress-bomb guard (≈35 MP)

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".tif", ".tiff"
    };

    public ImageStorageService(IWebHostEnvironment environment, ILogger<ImageStorageService> logger, IUploadAuditService uploadAuditService)
    {
        _environment = environment;
        _logger = logger;
        _uploadAuditService = uploadAuditService;
    }

    public async Task<ImageStorageResult> SaveAsWebpAsync(IFormFile file, string targetDirectory, string filePrefix, CancellationToken cancellationToken = default)
    {
        return await SaveAsWebpAsync(file, new ImageSaveRequest(
            TargetDirectory: targetDirectory,
            FilePrefix: filePrefix,
            Category: "image",
            QualityProfile: ImageQualityProfile.Default,
            GenerateThumbnails: false), cancellationToken);
    }

    public async Task<ImageStorageResult> SaveAsWebpAsync(IFormFile file, ImageSaveRequest request, CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("Bos dosya yuklenemez.");
        }

        var (maxBytes, maxDimension, quality, thumbs) = ResolveProfile(request.QualityProfile);
        var allowedBytes = Math.Min(DefaultMaxUploadBytes, maxBytes);
        if (file.Length > allowedBytes)
        {
            var maxKb = Math.Max(1, allowedBytes / 1024);
            throw new InvalidOperationException(
                $"Tek bir gorsel en fazla {maxKb} KB ({Math.Max(1, allowedBytes / (1024 * 1024))} MB) olabilir. JPG/PNG yuklenir; sunucu WebP'ye donusturur (otel/oda max 15360 KB).");
        }

        var extension = Path.GetExtension(file.FileName);
        var hasAllowedExtension = !string.IsNullOrWhiteSpace(extension) && AllowedExtensions.Contains(extension);
        var hasImageContentType = !string.IsNullOrWhiteSpace(file.ContentType)
            && file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

        if (!hasImageContentType && !hasAllowedExtension)
        {
            throw new InvalidOperationException("Yalnizca gorsel dosyalari yuklenebilir.");
        }

        var targetDirectory = EnsureSafeTargetDirectory(request.TargetDirectory);
        Directory.CreateDirectory(targetDirectory);

        await using var inputStream = file.OpenReadStream();
        await ValidateMagicHeaderAsync(inputStream, cancellationToken);
        if (inputStream.CanSeek) inputStream.Position = 0;

        var info = await Image.IdentifyAsync(inputStream, cancellationToken);
        if (info is null)
        {
            throw new InvalidOperationException("Gorsel okunamadi.");
        }
        if ((long)info.Width * info.Height > MaxPixelCount)
        {
            throw new InvalidOperationException("Gorsel boyutu cok buyuk. Daha kucuk bir gorsel secin.");
        }

        if (inputStream.CanSeek) inputStream.Position = 0;
        using var image = await Image.LoadAsync(inputStream, cancellationToken);
        image.Mutate(context =>
        {
            context.AutoOrient();

            if (image.Width > maxDimension || image.Height > maxDimension)
            {
                context.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(maxDimension, maxDimension)
                });
            }
        });

        var baseName = $"{request.FilePrefix}-{Guid.NewGuid():N}";
        var fileName = $"{baseName}.webp";
        var physicalPath = Path.Combine(targetDirectory, fileName);
        var encoder = new WebpEncoder
        {
            FileFormat = WebpFileFormatType.Lossy,
            Quality = quality
        };

        await AtomicFileWriter.WriteFileAtomicAsync(physicalPath, async (stream, ct) =>
        {
            await image.SaveAsync(stream, encoder, ct);
        }, cancellationToken);

        var fileInfo = new FileInfo(physicalPath);
        var sha = await AtomicFileWriter.ComputeSha256Async(physicalPath, cancellationToken);
        var variants = new List<ImageVariant>();

        if (request.GenerateThumbnails && thumbs.Length > 0)
        {
            foreach (var w in thumbs)
            {
                var resized = image.Clone(ctx =>
                {
                    ctx.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(w, w)
                    });
                });

                var thumbName = $"{baseName}-w{w}.webp";
                var thumbPath = Path.Combine(targetDirectory, thumbName);
                await AtomicFileWriter.WriteFileAtomicAsync(thumbPath, async (stream, ct) =>
                {
                    await resized.SaveAsync(stream, encoder, ct);
                }, cancellationToken);
                var ti = new FileInfo(thumbPath);
                variants.Add(new ImageVariant($"w{w}", thumbName, resized.Width, resized.Height, ti.Length));
            }
        }

        try
        {
            await _uploadAuditService.RecordAsync(new UploadAuditEvent(
                Kind: "image",
                Category: request.Category,
                SizeBytes: file.Length,
                StoredName: fileName,
                StoredPathOrUrl: physicalPath,
                ContentType: string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                Extension: ".webp",
                Sha256: sha,
                OwnerUserId: request.OwnerUserId,
                OwnerFirmaId: request.OwnerFirmaId,
                ContextTable: request.ContextTable,
                ContextId: request.ContextId,
                RemoteIp: null,
                UserAgent: null
            ), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Upload audit log failed for {Path}", physicalPath);
        }

        return new ImageStorageResult
        {
            FileName = fileName,
            FileSizeBytes = fileInfo.Length,
            Width = image.Width,
            Height = image.Height,
            Sha256 = sha,
            Variants = variants
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

    /// <summary>Max upload bytes, longest edge px, WebP quality, thumbnail widths.</summary>
    private static (long MaxBytes, int MaxDimension, int WebpQuality, int[] ThumbWidths) ResolveProfile(ImageQualityProfile profile)
    {
        return profile switch
        {
            // 6144 KB input max; WebP ~88
            ImageQualityProfile.Avatar => (MaxBytes: 6 * 1024 * 1024, MaxDimension: 1024, WebpQuality: 88, ThumbWidths: new[] { 96, 192, 384 }),
            // 15360 KB; 2560px edge; thumbs 480/960/1440
            ImageQualityProfile.HotelPhoto => (MaxBytes: 15 * 1024 * 1024, MaxDimension: 2560, WebpQuality: 90, ThumbWidths: new[] { 480, 960, 1440 }),
            ImageQualityProfile.RoomPhoto => (MaxBytes: 15 * 1024 * 1024, MaxDimension: 2560, WebpQuality: 90, ThumbWidths: new[] { 480, 960, 1440 }),
            ImageQualityProfile.RequestVisual => (MaxBytes: 12 * 1024 * 1024, MaxDimension: 2200, WebpQuality: 88, ThumbWidths: new[] { 480, 960 }),
            _ => (MaxBytes: DefaultMaxUploadBytes, MaxDimension: DefaultMaxDimension, WebpQuality: DefaultWebpQuality, ThumbWidths: Array.Empty<int>())
        };
    }

    private string EnsureSafeTargetDirectory(string targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            throw new InvalidOperationException("Hedef dizin gecersiz.");
        }

        var full = Path.GetFullPath(targetDirectory);
        var webRoot = Path.GetFullPath(_environment.WebRootPath);
        // Görsel uploadlar sadece webroot altına yazılmalı (CDN-ready public assets).
        if (!full.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Hedef dizin guvenli degil.");
        }

        return full;
    }

    private static async Task ValidateMagicHeaderAsync(Stream stream, CancellationToken cancellationToken)
    {
        var header = new byte[16];
        var read = 0;
        while (read < header.Length)
        {
            var r = await stream.ReadAsync(header.AsMemory(read, header.Length - read), cancellationToken);
            if (r <= 0) break;
            read += r;
        }

        if (read < 12)
        {
            throw new InvalidOperationException("Gorsel dosyasi gecersiz.");
        }

        // JPEG: FF D8 FF
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return;
        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) return;
        // GIF: 47 49 46 38
        if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38) return;
        // WEBP: RIFF....WEBP
        if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
            && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50) return;
        // BMP: 42 4D
        if (header[0] == 0x42 && header[1] == 0x4D) return;
        // TIFF: 49 49 2A 00 or 4D 4D 00 2A
        if ((header[0] == 0x49 && header[1] == 0x49 && header[2] == 0x2A && header[3] == 0x00)
            || (header[0] == 0x4D && header[1] == 0x4D && header[2] == 0x00 && header[3] == 0x2A)) return;

        throw new InvalidOperationException("Gorsel dosyasi gecersiz.");
    }
}
