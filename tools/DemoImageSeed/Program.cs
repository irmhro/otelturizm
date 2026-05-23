using System.Globalization;
using Microsoft.Data.SqlClient;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

const string DefaultConn =
    "Server=(localdb)\\MSSQLLocalDB;Database=otelturizm_2026db;Trusted_Connection=True;TrustServerCertificate=True;";

var repoRoot = args.FirstOrDefault(a => a.StartsWith("--root=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1]
    ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
var webRoot = Path.Combine(repoRoot, "wwwroot");
var connectionString = args.FirstOrDefault(a => a.StartsWith("--conn=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1]
    ?? DefaultConn;
var skipDb = args.Any(a => string.Equals(a, "--skip-db", StringComparison.OrdinalIgnoreCase));
var codesArg = args.FirstOrDefault(a => a.StartsWith("--codes=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1];
var codeFilter = string.IsNullOrWhiteSpace(codesArg)
    ? null
    : codesArg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(c => !string.IsNullOrWhiteSpace(c))
        .ToArray();

var legacySeedCodes = new[]
{
    "ORK-SEED-001", "ORK-SEED-002", "ORK-SEED-003", "ORK-SEED-004", "ORK-SEED-005",
    "ORK-SEED-006", "ORK-SEED-007", "ORK-SEED-008", "ORK-SEED-009", "ORK-SEED-010"
};

var hotelGallery = new[] { "demo-cover.webp", "demo-01.webp", "demo-02.webp", "demo-03.webp" };
var roomFiles = new[] { "demo-room-cover.webp", "demo-room-02.webp" };

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("Otelturizm-DemoImageSeed/2.0");

var hotels = skipDb
    ? BuildSkipDbHotels(codeFilter, legacySeedCodes)
    : await LoadHotelsAsync(connectionString, codeFilter);

if (hotels.Count == 0)
{
    if (skipDb)
    {
        Console.Error.WriteLine("Islenecek otel kodu yok (--codes= veya DB modu kullanin).");
        return 2;
    }

    Console.Error.WriteLine(
        "DB'de ORK-IST-% / ORK-SEED-% otel bulunamadi. Once istanbul ilce seed SQL uygulayin.");
    return 2;
}

var encoder = new WebpEncoder { Quality = 88 };
var downloaded = 0;
var hotelsProcessed = 0;

foreach (var hotel in hotels.OrderBy(h => h.Code, StringComparer.OrdinalIgnoreCase))
{
    var useDbPaths = hotel.HotelId > 0;
    if (!useDbPaths && !skipDb)
    {
        Console.WriteLine($"Atlandi (DB yok): {hotel.Code}");
        continue;
    }

    var hotelDir = useDbPaths
        ? Path.Combine(webRoot, "uploads", "images", hotel.HotelId.ToString(CultureInfo.InvariantCulture), "hotel")
        : Path.Combine(webRoot, "uploads", "images", "demo-seed", hotel.Code, "hotel");

    Directory.CreateDirectory(hotelDir);

    await DownloadWebpAsync(http, $"{hotel.Code}-cover", Path.Combine(hotelDir, hotelGallery[0]), encoder);
    downloaded++;
    for (var i = 1; i <= 3; i++)
    {
        await DownloadWebpAsync(http, $"{hotel.Code}-g{i}", Path.Combine(hotelDir, hotelGallery[i]), encoder);
        downloaded++;
    }

    var roomIds = hotel.RoomIds.Count > 0
        ? hotel.RoomIds
        : new List<long> { 1 };

    var roomIndex = 0;
    foreach (var roomId in roomIds.Take(2))
    {
        roomIndex++;
        var roomDir = useDbPaths
            ? Path.Combine(webRoot, "uploads", "images", hotel.HotelId.ToString(CultureInfo.InvariantCulture), "rooms",
                roomId.ToString(CultureInfo.InvariantCulture))
            : Path.Combine(webRoot, "uploads", "images", "demo-seed", hotel.Code, "rooms",
                roomId.ToString(CultureInfo.InvariantCulture));

        Directory.CreateDirectory(roomDir);
        for (var r = 0; r < roomFiles.Length; r++)
        {
            await DownloadWebpAsync(http, $"{hotel.Code}-room{roomIndex}-{r + 1}",
                Path.Combine(roomDir, roomFiles[r]), encoder);
            downloaded++;
        }
    }

    hotelsProcessed++;
    Console.WriteLine($"OK {hotel.Code} -> {hotelDir} ({Math.Min(roomIds.Count, 2)} oda tipi)");
}

Console.WriteLine($"Islenen otel: {hotelsProcessed}");
Console.WriteLine($"Indirilen/guncellenen gorsel: {downloaded}");
return 0;

static List<HotelSeedRow> BuildSkipDbHotels(string[]? codeFilter, string[] legacySeedCodes)
{
    var codes = codeFilter is { Length: > 0 } ? codeFilter : legacySeedCodes;
    return codes.Select(c => new HotelSeedRow(c, 0, new List<long> { 1 })).ToList();
}

static async Task DownloadWebpAsync(HttpClient http, string seed, string destPath, WebpEncoder encoder)
{
    var url = $"https://picsum.photos/seed/{Uri.EscapeDataString(seed)}/1440/960.jpg";
    await using var stream = await http.GetStreamAsync(url);
    using var image = await Image.LoadAsync(stream);
    image.Mutate(x => x.AutoOrient());
    if (image.Width > 1920 || image.Height > 1280)
    {
        image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(1920, 1280) }));
    }

    var dir = Path.GetDirectoryName(destPath)!;
    Directory.CreateDirectory(dir);
    if (File.Exists(destPath))
    {
        File.Delete(destPath);
    }

    await using var fs = new FileStream(destPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
    await image.SaveAsWebpAsync(fs, encoder);
}

static async Task<List<HotelSeedRow>> LoadHotelsAsync(string connectionString, string[]? codeFilter)
{
    var list = new List<HotelSeedRow>();
    await using var conn = new SqlConnection(connectionString);
    await conn.OpenAsync();

    var hotelSql = @"
        SELECT o.[ID], o.[OTEL_KODU]
        FROM [dbo].[OTELLER] o
        WHERE (o.[OTEL_KODU] LIKE N'ORK-IST-%' OR o.[OTEL_KODU] LIKE N'ORK-SEED-%')";

    if (codeFilter is { Length: > 0 })
    {
        var placeholders = string.Join(", ", codeFilter.Select((_, i) => $"@c{i}"));
        hotelSql += $" AND o.[OTEL_KODU] IN ({placeholders})";
    }

    hotelSql += " ORDER BY o.[OTEL_KODU];";

    await using (var cmd = new SqlCommand(hotelSql, conn))
    {
        if (codeFilter is { Length: > 0 })
        {
            for (var i = 0; i < codeFilter.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@c{i}", codeFilter[i]);
            }
        }

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new HotelSeedRow(reader.GetString(1), reader.GetInt64(0), new List<long>()));
        }
    }

    const string roomSql = @"
        SELECT TOP (2) r.[ID]
        FROM [dbo].[ODA_TIPLERI] r
        WHERE r.[OTEL_ID] = @otelId
        ORDER BY r.[SIRALAMA], r.[ID];";

    foreach (var hotel in list)
    {
        await using var roomCmd = new SqlCommand(roomSql, conn);
        roomCmd.Parameters.AddWithValue("@otelId", hotel.HotelId);
        await using var roomReader = await roomCmd.ExecuteReaderAsync();
        while (await roomReader.ReadAsync())
        {
            hotel.RoomIds.Add(roomReader.GetInt64(0));
        }
    }

    return list;
}

sealed record HotelSeedRow(string Code, long HotelId, List<long> RoomIds);
