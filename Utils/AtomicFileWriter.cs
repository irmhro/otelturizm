using System.Security.Cryptography;

namespace otelturizmnew.Utils;

public static class AtomicFileWriter
{
    public static async Task WriteFileAtomicAsync(
        string absolutePath,
        Func<Stream, CancellationToken, Task> write,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            throw new ArgumentException("Path is required.", nameof(absolutePath));
        }

        var directory = Path.GetDirectoryName(absolutePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Directory not resolved.");
        }

        Directory.CreateDirectory(directory);
        var tempPath = absolutePath + ".tmp-" + Guid.NewGuid().ToString("N");

        try
        {
            await using (var fs = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true))
            {
                await write(fs, cancellationToken);
                await fs.FlushAsync(cancellationToken);
            }

            if (File.Exists(absolutePath))
            {
                // Windows için güvenli replace
                File.Replace(tempPath, absolutePath, null);
            }
            else
            {
                File.Move(tempPath, absolutePath);
            }
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
                // best-effort
            }
        }
    }

    public static async Task CopyFromFormFileAtomicAsync(
        IFormFile file,
        string absolutePath,
        CancellationToken cancellationToken = default)
    {
        await WriteFileAtomicAsync(absolutePath, async (stream, ct) =>
        {
            await using var input = file.OpenReadStream();
            await input.CopyToAsync(stream, ct);
        }, cancellationToken);
    }

    public static async Task<string> ComputeSha256Async(string absolutePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(absolutePath);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}

