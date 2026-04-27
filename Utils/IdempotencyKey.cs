using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace otelturizmnew.Utils;

public static class IdempotencyKey
{
    public static string ForObject(string prefix, object? value)
    {
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            WriteIndented = false
        });
        return prefix + ":" + HashShort(json);
    }

    public static string HashShort(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash[..8]);
    }
}

