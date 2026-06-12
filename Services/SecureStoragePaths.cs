using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace otelturizmnew.Services;

internal static class SecureStoragePaths
{
    public static string ResolveRoot(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["SecureStorage:RootPath"]?.Trim();
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured);
        }

        return Path.Combine(environment.ContentRootPath, "App_Data", "secure-storage");
    }

    public static string BuildCategoryRoot(string storageRoot, string safeCategory, long? ownerUserId)
    {
        if (string.Equals(safeCategory, "profile", StringComparison.OrdinalIgnoreCase)
            && ownerUserId.HasValue
            && ownerUserId.Value > 0)
        {
            return Path.Combine(
                storageRoot,
                "profile",
                ownerUserId.Value.ToString(CultureInfo.InvariantCulture));
        }

        return Path.Combine(storageRoot, safeCategory);
    }

    public static bool TryEnsureWritable(string storageRoot, out string? errorMessage)
    {
        errorMessage = null;
        try
        {
            Directory.CreateDirectory(storageRoot);
            Directory.CreateDirectory(Path.Combine(storageRoot, "profile"));

            var probeDir = Path.Combine(storageRoot, "profile", ".probe");
            Directory.CreateDirectory(probeDir);
            var probeFile = Path.Combine(probeDir, "write-test.tmp");
            File.WriteAllText(probeFile, "ok");
            File.Delete(probeFile);
            Directory.Delete(probeDir, recursive: true);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }
}
