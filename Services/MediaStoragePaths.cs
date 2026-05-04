using System.Globalization;

namespace otelturizmnew.Services;

internal static class MediaStoragePaths
{
    public static string HotelImagesDirectory(string webRootPath, long hotelId) =>
        Path.Combine(webRootPath, "uploads", "images", hotelId.ToString(CultureInfo.InvariantCulture), "hotel");

    public static string HotelImagesUrl(long hotelId, string fileName) =>
        $"/uploads/images/{hotelId.ToString(CultureInfo.InvariantCulture)}/hotel/{fileName}";

    public static string RoomImagesDirectory(string webRootPath, long hotelId, long roomId) =>
        Path.Combine(webRootPath, "uploads", "images", hotelId.ToString(CultureInfo.InvariantCulture), "rooms", roomId.ToString(CultureInfo.InvariantCulture));

    public static string RoomImagesUrl(long hotelId, long roomId, string fileName) =>
        $"/uploads/images/{hotelId.ToString(CultureInfo.InvariantCulture)}/rooms/{roomId.ToString(CultureInfo.InvariantCulture)}/{fileName}";

    public static string HotelFilesDirectory(string webRootPath, long hotelId, string category) =>
        Path.Combine(webRootPath, "uploads", "file", hotelId.ToString(CultureInfo.InvariantCulture), NormalizeSegment(category));

    private static string NormalizeSegment(string value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "genel" : value.Trim().ToLowerInvariant();
        var chars = text.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray();
        return string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
    }
}
