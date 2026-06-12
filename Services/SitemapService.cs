using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;
using otelturizmnew.Models.Seo;
using otelturizmnew.Services.Abstractions;
using otelturizmnew.Utils;

namespace otelturizmnew.Services;

public sealed class SitemapService : ISitemapService
{
    private static readonly SemaphoreSlim SyncLock = new(1, 1);
    private static readonly string[] SitemapLocales = ["tr-TR", "en-US", "en-GB", "de-DE", "fr-FR", "es-ES"];
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromDays(3);
    private static readonly string[] SubSitemapFiles =
    [
        "static.xml",
        "hotels.xml",
        "rooms.xml",
        "campaigns.xml",
        "blog.xml",
        "locations.xml"
    ];

    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public SitemapService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public async Task EnsureFreshSitemapAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        var filePath = GetSitemapFilePath();
        if (!force && File.Exists(filePath))
        {
            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(filePath);
            if (age < RefreshInterval)
            {
                return;
            }
        }

        await SyncLock.WaitAsync(cancellationToken);
        try
        {
            if (!force && File.Exists(filePath))
            {
                var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(filePath);
                if (age < RefreshInterval)
                {
                    return;
                }
            }

            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            await using var lockHandle = await TryAcquireFileLockAsync(GetSitemapLockFilePath(), force, cancellationToken);
            if (lockHandle is null)
            {
                return;
            }

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var now = DateTime.UtcNow;
            var regionalFiles = await BuildRegionalSitemapsAsync(connection, cancellationToken);
            var staticEntries = BuildStaticEntries().ToList();
            var hotelEntries = await GetHotelEntriesAsync(connection, cancellationToken);
            var roomEntries = await GetRoomEntriesAsync(connection, cancellationToken);
            var campaignEntries = await GetCampaignEntriesAsync(connection, cancellationToken);
            var blogEntries = await GetBlogEntriesAsync(connection, cancellationToken);
            var locationEntries = await GetLocationEntriesAsync(connection, cancellationToken);

            var subSitemaps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["static.xml"] = BuildUrlSetXml(staticEntries),
                ["hotels.xml"] = BuildUrlSetXml(hotelEntries),
                ["rooms.xml"] = BuildUrlSetXml(roomEntries),
                ["campaigns.xml"] = BuildUrlSetXml(campaignEntries),
                ["blog.xml"] = BuildUrlSetXml(blogEntries),
                ["locations.xml"] = BuildUrlSetXml(locationEntries)
            };

            await SaveSubSitemapsAsync(subSitemaps, cancellationToken);
            await SaveRegionalSitemapsAsync(regionalFiles, cancellationToken);

            var priceFeedJson = await BuildHotelOffersFeedJsonAsync(connection, cancellationToken);
            await SavePriceFeedAsync(priceFeedJson, cancellationToken);

            var indexEntries = new List<SitemapIndexEntry>
            {
                new("sitemaps/static.xml", now, staticEntries.Count),
                new("sitemaps/hotels.xml", ResolveLastModifiedUtc(hotelEntries, now), hotelEntries.Count),
                new("sitemaps/rooms.xml", ResolveLastModifiedUtc(roomEntries, now), roomEntries.Count),
                new("sitemaps/campaigns.xml", ResolveLastModifiedUtc(campaignEntries, now), campaignEntries.Count),
                new("sitemaps/blog.xml", ResolveLastModifiedUtc(blogEntries, now), blogEntries.Count),
                new("sitemaps/locations.xml", ResolveLastModifiedUtc(locationEntries, now), locationEntries.Count),
                new("feeds/hotel-offers.json", now, 0)
            };

            foreach (var regional in regionalFiles)
            {
                indexEntries.Add(new SitemapIndexEntry(
                    "xml/" + regional.FileName,
                    now,
                    CountUrlEntriesFromXml(regional.XmlContent)));
            }

            var indexXml = BuildSitemapIndexXml(indexEntries);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await AtomicFileWriter.WriteFileAtomicAsync(filePath, async (stream, ct) =>
            {
                var bytes = new UTF8Encoding(false).GetBytes(indexXml);
                await stream.WriteAsync(bytes, ct);
            }, cancellationToken);
        }
        finally
        {
            SyncLock.Release();
        }
    }

    public async Task<string> GetSitemapXmlAsync(CancellationToken cancellationToken = default)
    {
        await EnsureFreshSitemapAsync(false, cancellationToken);
        var filePath = GetSitemapFilePath();
        if (File.Exists(filePath))
        {
            return await File.ReadAllTextAsync(filePath, cancellationToken);
        }

        return BuildSitemapIndexXml([]);
    }

    public async Task<string?> GetSubSitemapXmlAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !IsSafeXmlSlug(fileName))
        {
            return null;
        }

        if (!SubSitemapFiles.Contains(fileName + ".xml", StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        await EnsureFreshSitemapAsync(false, cancellationToken);
        var path = Path.Combine(GetSubSitemapDirectoryPath(), fileName + ".xml");
        if (!File.Exists(path))
        {
            return null;
        }

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    public async Task<string?> GetRegionalSitemapXmlAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !IsSafeXmlSlug(fileName))
        {
            return null;
        }

        await EnsureFreshSitemapAsync(false, cancellationToken);
        var path = Path.Combine(GetRegionalSitemapDirectoryPath(), fileName + ".xml");
        if (!File.Exists(path))
        {
            return null;
        }

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    public async Task<string?> GetHotelOffersFeedJsonAsync(CancellationToken cancellationToken = default)
    {
        await EnsureFreshSitemapAsync(false, cancellationToken);
        var path = GetPriceFeedFilePath();
        if (!File.Exists(path))
        {
            return null;
        }

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    public async Task<SitemapDiagnosticsViewModel> GetDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureFreshSitemapAsync(false, cancellationToken);

        var model = new SitemapDiagnosticsViewModel
        {
            MainSitemapPhysicalPath = GetSitemapFilePath(),
            PriceFeedUrl = BuildAbsoluteUrl("/feeds/hotel-offers.json")
        };

        if (File.Exists(model.MainSitemapPhysicalPath))
        {
            model.LastRefreshUtc = File.GetLastWriteTimeUtc(model.MainSitemapPhysicalPath);
            model.MainSitemapUrlCount = CountXmlEntries(model.MainSitemapPhysicalPath);
            model.SubSitemapCount = model.MainSitemapUrlCount;
            model.Files.Add(new SitemapFileSummaryViewModel
            {
                FileName = Path.GetFileName(model.MainSitemapPhysicalPath),
                PhysicalPath = model.MainSitemapPhysicalPath,
                PublicUrl = BuildAbsoluteUrl("/sitemap.xml"),
                ScopeText = "Ana sitemap index",
                LastModifiedUtc = model.LastRefreshUtc,
                UrlCount = model.MainSitemapUrlCount
            });
        }

        var subDirectory = GetSubSitemapDirectoryPath();
        if (Directory.Exists(subDirectory))
        {
            foreach (var subPath in Directory.EnumerateFiles(subDirectory, "*.xml", SearchOption.TopDirectoryOnly).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileName(subPath);
                var slug = Path.GetFileNameWithoutExtension(subPath);
                if (!IsSafeXmlSlug(slug))
                {
                    continue;
                }

                var urlCount = CountXmlEntries(subPath);
                var scope = ResolveSubSitemapScope(fileName);
                model.Files.Add(new SitemapFileSummaryViewModel
                {
                    FileName = fileName,
                    PhysicalPath = subPath,
                    PublicUrl = BuildAbsoluteUrl("/sitemaps/" + slug + ".xml"),
                    ScopeText = scope,
                    LastModifiedUtc = File.GetLastWriteTimeUtc(subPath),
                    UrlCount = urlCount
                });

                switch (fileName.ToLowerInvariant())
                {
                    case "hotels.xml":
                        model.HotelUrlCount = urlCount;
                        break;
                    case "rooms.xml":
                        model.RoomUrlCount = urlCount;
                        break;
                    case "campaigns.xml":
                        model.CampaignUrlCount = urlCount;
                        break;
                    case "blog.xml":
                        model.BlogUrlCount = urlCount;
                        break;
                    case "locations.xml":
                        model.LocationUrlCount = urlCount;
                        break;
                }
            }
        }

        var priceFeedPath = GetPriceFeedFilePath();
        if (File.Exists(priceFeedPath))
        {
            model.PriceFeedOfferCount = CountPriceFeedOffers(priceFeedPath);
            model.Files.Add(new SitemapFileSummaryViewModel
            {
                FileName = Path.GetFileName(priceFeedPath),
                PhysicalPath = priceFeedPath,
                PublicUrl = model.PriceFeedUrl,
                ScopeText = "Otel/oda fiyat feed (Schema.org)",
                LastModifiedUtc = File.GetLastWriteTimeUtc(priceFeedPath),
                UrlCount = model.PriceFeedOfferCount
            });
        }

        var regionalDirectory = GetRegionalSitemapDirectoryPath();
        if (Directory.Exists(regionalDirectory))
        {
            foreach (var regionalPath in Directory.EnumerateFiles(regionalDirectory, "*.xml", SearchOption.TopDirectoryOnly).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileName(regionalPath);
                var slug = Path.GetFileNameWithoutExtension(regionalPath);
                if (!IsSafeXmlSlug(slug))
                {
                    continue;
                }

                var urlCount = CountXmlEntries(regionalPath);
                model.Files.Add(new SitemapFileSummaryViewModel
                {
                    FileName = fileName,
                    PhysicalPath = regionalPath,
                    PublicUrl = BuildAbsoluteUrl("/xml/" + slug + ".xml"),
                    ScopeText = fileName.Contains("-oteller", StringComparison.OrdinalIgnoreCase)
                        ? "İl/ilçe otelleri"
                        : "İl otelleri",
                    LastModifiedUtc = File.GetLastWriteTimeUtc(regionalPath),
                    UrlCount = urlCount
                });
            }
        }

        model.RegionalFileCount = model.Files.Count(static x =>
            x.PublicUrl.Contains("/xml/", StringComparison.OrdinalIgnoreCase));
        model.RegionalUrlCount = model.Files
            .Where(static x => x.PublicUrl.Contains("/xml/", StringComparison.OrdinalIgnoreCase))
            .Sum(static x => x.UrlCount);

        return model;
    }

    private static string ResolveSubSitemapScope(string fileName) => fileName.ToLowerInvariant() switch
    {
        "static.xml" => "Statik sayfalar",
        "hotels.xml" => "Otel detay sayfaları",
        "rooms.xml" => "Oda detay URL'leri",
        "campaigns.xml" => "Kampanyalar",
        "blog.xml" => "Blog yazıları",
        "locations.xml" => "Şehir/ilçe listeleme",
        _ => "Alt sitemap"
    };

    private string BuildSitemapIndexXml(IReadOnlyCollection<SitemapIndexEntry> entries)
    {
        var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");
        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(ns + "sitemapindex",
                entries
                    .Where(static x => !string.IsNullOrWhiteSpace(x.RelativePath))
                    .Select(entry => new XElement(ns + "sitemap",
                        new XElement(ns + "loc", BuildAbsoluteUrl("/" + entry.RelativePath.TrimStart('/'))),
                        new XElement(ns + "lastmod", entry.LastModifiedUtc.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture))))));

        return document.ToString(SaveOptions.DisableFormatting);
    }

    private string BuildUrlSetXml(IReadOnlyCollection<SitemapUrlEntry> entries)
    {
        var urlNs = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");
        var imageNs = XNamespace.Get("http://www.google.com/schemas/sitemap-image/1.1");
        var xhtmlNs = XNamespace.Get("http://www.w3.org/1999/xhtml");

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(urlNs + "urlset",
                new XAttribute(XNamespace.Xmlns + "image", imageNs),
                new XAttribute(XNamespace.Xmlns + "xhtml", xhtmlNs),
                entries
                    .Where(x => !string.IsNullOrWhiteSpace(x.Location))
                    .GroupBy(x => x.Location, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.OrderByDescending(y => y.LastModifiedUtc ?? DateTime.MinValue).First())
                    .Select(entry =>
                    {
                        var urlElement = new XElement(urlNs + "url",
                            new XElement(urlNs + "loc", entry.Location),
                            new XElement(urlNs + "changefreq", entry.ChangeFrequency),
                            new XElement(urlNs + "priority", entry.Priority.ToString("0.0", CultureInfo.InvariantCulture)));

                        if (entry.LastModifiedUtc.HasValue)
                        {
                            urlElement.Add(new XElement(urlNs + "lastmod", entry.LastModifiedUtc.Value.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture)));
                        }

                        if (entry.IncludeHrefLang)
                        {
                            foreach (var locale in SitemapLocales)
                            {
                                urlElement.Add(new XElement(xhtmlNs + "link",
                                    new XAttribute("rel", "alternate"),
                                    new XAttribute("hreflang", locale),
                                    new XAttribute("href", BuildLocalizedUrl(entry.Location, locale))));
                            }

                            urlElement.Add(new XElement(xhtmlNs + "link",
                                new XAttribute("rel", "alternate"),
                                new XAttribute("hreflang", "x-default"),
                                new XAttribute("href", BuildLocalizedUrl(entry.Location, SitemapLocales[0]))));
                        }

                        var images = entry.Images.Count > 0
                            ? entry.Images
                            : (string.IsNullOrWhiteSpace(entry.ImageUrl)
                                ? []
                                : [new SitemapImageEntry { Url = entry.ImageUrl!, Title = entry.ImageTitle }]);

                        foreach (var image in images.Where(static x => !string.IsNullOrWhiteSpace(x.Url)).Take(5))
                        {
                            var imageElement = new XElement(imageNs + "image",
                                new XElement(imageNs + "loc", image.Url));

                            if (!string.IsNullOrWhiteSpace(image.Title))
                            {
                                imageElement.Add(new XElement(imageNs + "title", image.Title));
                            }

                            if (!string.IsNullOrWhiteSpace(image.Caption))
                            {
                                imageElement.Add(new XElement(imageNs + "caption", image.Caption));
                            }

                            urlElement.Add(imageElement);
                        }

                        return urlElement;
                    })));

        return document.ToString(SaveOptions.DisableFormatting);
    }

    private async Task<List<RegionalSitemapFile>> BuildRegionalSitemapsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                o.[OTEL_ADI],
                o.[OTEL_KODU],
                COALESCE(o.[SEHIR], '') AS [SEHIR],
                COALESCE(o.[ILCE], '') AS ilce,
                COALESCE(o.[GUNCELLENME_TARIHI], o.[ONAY_TARIHI], o.[OLUSTURULMA_TARIHI], SYSUTCDATETIME()) AS last_modified
            FROM [dbo].[OTELLER] o
            WHERE o.[YAYIN_DURUMU] = N'Yayında'
              AND o.[ONAY_DURUMU] IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
              AND COALESCE(NULLIF(o.[OTEL_ADI], ''), NULLIF(o.[OTEL_KODU], '')) <> '';
            """;

        var hotels = new List<RegionalHotelRow>();
        await using (var command = new SqlCommand(sql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var hotelName = NormalizeTurkishText(reader.IsDBNull(0) ? string.Empty : reader.GetString(0));
                var hotelCode = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                var city = NormalizeTurkishText(reader.IsDBNull(2) ? string.Empty : reader.GetString(2));
                var district = NormalizeTurkishText(reader.IsDBNull(3) ? string.Empty : reader.GetString(3));
                var slug = BuildHotelSlug(hotelName, hotelCode);
                if (string.IsNullOrWhiteSpace(slug))
                {
                    continue;
                }

                hotels.Add(new RegionalHotelRow(
                    city,
                    district,
                    BuildAbsoluteUrl("/hotel/" + slug),
                    DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc)));
            }
        }

        var result = new List<RegionalSitemapFile>();
        foreach (var cityGroup in hotels.Where(static x => !string.IsNullOrWhiteSpace(x.City)).GroupBy(x => x.City, StringComparer.OrdinalIgnoreCase))
        {
            var citySlug = BuildAreaSlug(cityGroup.Key);
            if (string.IsNullOrWhiteSpace(citySlug))
            {
                continue;
            }

            var cityFileName = citySlug + "otelleri.xml";
            result.Add(new RegionalSitemapFile(
                cityFileName,
                BuildRegionalUrlSetXml(cityGroup.Select(static x => (x.HotelUrl, x.LastModifiedUtc)).ToList()),
                BuildAbsoluteUrl("/xml/" + Path.GetFileNameWithoutExtension(cityFileName) + ".xml")));

            foreach (var districtGroup in cityGroup.Where(static x => !string.IsNullOrWhiteSpace(x.District)).GroupBy(x => x.District, StringComparer.OrdinalIgnoreCase))
            {
                var districtSlug = BuildAreaSlug(districtGroup.Key);
                if (string.IsNullOrWhiteSpace(districtSlug))
                {
                    continue;
                }

                var districtFileName = citySlug + "-" + districtSlug + "-oteller.xml";
                result.Add(new RegionalSitemapFile(
                    districtFileName,
                    BuildRegionalUrlSetXml(districtGroup.Select(static x => (x.HotelUrl, x.LastModifiedUtc)).ToList()),
                    BuildAbsoluteUrl("/xml/" + Path.GetFileNameWithoutExtension(districtFileName) + ".xml")));
            }
        }

        return result
            .GroupBy(x => x.FileName, StringComparer.OrdinalIgnoreCase)
            .Select(static x => x.First())
            .OrderBy(x => x.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildRegionalUrlSetXml(IReadOnlyCollection<(string Url, DateTime LastModifiedUtc)> items)
    {
        var urlNs = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");
        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(urlNs + "urlset",
                items
                    .Where(static x => !string.IsNullOrWhiteSpace(x.Url))
                    .GroupBy(x => x.Url, StringComparer.OrdinalIgnoreCase)
                    .Select(static x => x.OrderByDescending(y => y.LastModifiedUtc).First())
                    .Select(item => new XElement(urlNs + "url",
                        new XElement(urlNs + "loc", item.Url),
                        new XElement(urlNs + "lastmod", item.LastModifiedUtc.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture)),
                        new XElement(urlNs + "changefreq", "daily"),
                        new XElement(urlNs + "priority", "0.7")))));

        return document.ToString(SaveOptions.DisableFormatting);
    }

    private IEnumerable<SitemapUrlEntry> BuildStaticEntries()
    {
        var now = DateTime.UtcNow;
        return
        [
            CreateStatic("/", "daily", 1.0m, now),
            CreateStatic("/hotel", "daily", 0.9m, now),
            CreateStatic("/hotel/harita", "weekly", 0.75m, now),
            CreateStatic("/en/hotels", "daily", 0.85m, now),
            CreateStatic("/de/hotels", "daily", 0.85m, now),
            CreateStatic("/en/hotels/istanbul", "daily", 0.85m, now),
            CreateStatic("/de/hotels/istanbul", "daily", 0.85m, now),
            CreateStatic("/kampanyalar", "daily", 0.8m, now),
            CreateStatic("/deneyimler", "weekly", 0.75m, now),
            CreateStatic("/seyahat-planlama", "weekly", 0.7m, now),
            CreateStatic("/kurumsal", "weekly", 0.7m, now),
            CreateStatic("/hakkimizda", "monthly", 0.6m, now),
            CreateStatic("/kariyer", "monthly", 0.55m, now),
            CreateStatic("/basin-odasi", "monthly", 0.55m, now),
            CreateStatic("/blog", "weekly", 0.6m, now),
            CreateStatic("/firma", "weekly", 0.7m, now),
            CreateStatic("/yardim-merkezi", "weekly", 0.6m, now),
            CreateStatic("/sss", "weekly", 0.6m, now),
            CreateStatic("/Home/Privacy", "monthly", 0.3m, now)
        ];
    }

    private SitemapUrlEntry CreateStatic(string relativePath, string changeFrequency, decimal priority, DateTime lastModifiedUtc)
        => new()
        {
            Location = BuildAbsoluteUrl(relativePath),
            ChangeFrequency = changeFrequency,
            Priority = priority,
            LastModifiedUtc = lastModifiedUtc
        };

    private async Task<List<SitemapUrlEntry>> GetHotelEntriesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                o.[OTEL_KODU],
                o.[OTEL_ADI],
                COALESCE(o.[SEHIR], '') AS sehir,
                COALESCE(o.[ILCE], '') AS ilce,
                COALESCE(o.[GUNCELLENME_TARIHI], o.[ONAY_TARIHI], o.[OLUSTURULMA_TARIHI], SYSUTCDATETIME()) AS last_modified,
                COALESCE(NULLIF(o.[KAPAK_FOTOGRAFI], ''), NULLIF(og.[GORSEL_URL], '')) AS image_url,
                COALESCE(o.[KISA_ACIKLAMA], '') AS short_description,
                COALESCE(room_stats.min_price, 0) AS min_price
            FROM [dbo].[OTELLER] o
            LEFT JOIN (
                SELECT g1.[OTEL_ID], g1.[GORSEL_URL]
                FROM (
                    SELECT
                        g.[OTEL_ID],
                        g.[GORSEL_URL],
                        ROW_NUMBER() OVER (PARTITION BY g.[OTEL_ID] ORDER BY g.[KAPAK_FOTOGRAFI_MI] DESC, g.[ONE_CIKAN] DESC, g.[SIRALAMA] ASC, g.id ASC) AS rn
                    FROM [dbo].[OTEL_GORSELLERI] g
                    WHERE g.[ONAY_DURUMU] IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
                      AND g.[GORSEL_URL] NOT LIKE '/uploads/logo/%'
                ) g1
                WHERE g1.rn = 1
            ) og ON og.[OTEL_ID] = o.id
            LEFT JOIN (
                SELECT
                    ot.[OTEL_ID],
                    MIN(ot.[STANDART_GECELIK_FIYAT]) AS min_price
                FROM [dbo].[ODA_TIPLERI] ot
                WHERE ot.[AKTIF_MI] = 1 AND ot.[STANDART_GECELIK_FIYAT] > 0
                GROUP BY ot.[OTEL_ID]
            ) room_stats ON room_stats.[OTEL_ID] = o.id
            WHERE o.[YAYIN_DURUMU] = N'Yayında'
              AND o.[ONAY_DURUMU] IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
              AND COALESCE(NULLIF(o.[OTEL_ADI], ''), NULLIF(o.[OTEL_KODU], '')) <> ''
            ORDER BY o.id DESC;
            """;

        var entries = new List<SitemapUrlEntry>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var hotelCode = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            var name = NormalizeTurkishText(reader.IsDBNull(1) ? string.Empty : reader.GetString(1));
            var city = NormalizeTurkishText(reader.IsDBNull(2) ? string.Empty : reader.GetString(2));
            var district = NormalizeTurkishText(reader.IsDBNull(3) ? string.Empty : reader.GetString(3));
            var lastModifiedUtc = reader.GetDateTime(4);
            var imageUrl = reader.IsDBNull(5) ? null : NormalizeImageUrl(reader.GetString(5));
            var shortDescription = NormalizeTurkishText(reader.IsDBNull(6) ? string.Empty : reader.GetString(6));
            var slug = BuildHotelSlug(name, hotelCode);

            if (string.IsNullOrWhiteSpace(slug))
            {
                continue;
            }

            var caption = string.IsNullOrWhiteSpace(shortDescription)
                ? $"{name} · {district}, {city}"
                : shortDescription;

            entries.Add(new SitemapUrlEntry
            {
                Location = BuildAbsoluteUrl("/hotel/" + slug),
                LastModifiedUtc = DateTime.SpecifyKind(lastModifiedUtc, DateTimeKind.Utc),
                ChangeFrequency = "daily",
                Priority = 0.85m,
                ImageUrl = imageUrl,
                ImageTitle = name,
                Images =
                [
                    new SitemapImageEntry
                    {
                        Url = imageUrl ?? string.Empty,
                        Title = name,
                        Caption = caption
                    }
                ]
            });
        }

        return entries;
    }

    private async Task<List<SitemapUrlEntry>> GetRoomEntriesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                o.[OTEL_KODU],
                o.[OTEL_ADI],
                ot.[ID] AS room_type_id,
                ot.[ODA_ADI],
                ot.[STANDART_GECELIK_FIYAT],
                COALESCE(ot.[GUNCELLENME_TARIHI], ot.[OLUSTURULMA_TARIHI], SYSUTCDATETIME()) AS last_modified,
                COALESCE(NULLIF(ot.[KAPAK_FOTOGRAFI], ''), NULLIF(o.[KAPAK_FOTOGRAFI], ''), NULLIF(og.[GORSEL_URL], '')) AS image_url
            FROM [dbo].[ODA_TIPLERI] ot
            INNER JOIN [dbo].[OTELLER] o ON o.[ID] = ot.[OTEL_ID]
            LEFT JOIN (
                SELECT g1.[OTEL_ID], g1.[GORSEL_URL]
                FROM (
                    SELECT
                        g.[OTEL_ID],
                        g.[GORSEL_URL],
                        ROW_NUMBER() OVER (PARTITION BY g.[OTEL_ID] ORDER BY g.[KAPAK_FOTOGRAFI_MI] DESC, g.[ONE_CIKAN] DESC, g.[SIRALAMA] ASC, g.id ASC) AS rn
                    FROM [dbo].[OTEL_GORSELLERI] g
                    WHERE g.[ONAY_DURUMU] IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
                      AND g.[GORSEL_URL] NOT LIKE '/uploads/logo/%'
                ) g1
                WHERE g1.rn = 1
            ) og ON og.[OTEL_ID] = o.id
            WHERE o.[YAYIN_DURUMU] = N'Yayında'
              AND o.[ONAY_DURUMU] IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
              AND ot.[AKTIF_MI] = 1
              AND ot.[STANDART_GECELIK_FIYAT] > 0
              AND COALESCE(NULLIF(o.[OTEL_ADI], ''), NULLIF(o.[OTEL_KODU], '')) <> ''
            ORDER BY o.id DESC, ot.[SIRALAMA] ASC, ot.id ASC;
            """;

        var entries = new List<SitemapUrlEntry>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var hotelCode = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            var hotelName = NormalizeTurkishText(reader.IsDBNull(1) ? string.Empty : reader.GetString(1));
            var roomTypeId = reader.GetInt64(2);
            var roomName = NormalizeTurkishText(reader.IsDBNull(3) ? string.Empty : reader.GetString(3));
            var price = reader.GetDecimal(4);
            var lastModifiedUtc = reader.GetDateTime(5);
            var imageUrl = reader.IsDBNull(6) ? null : NormalizeImageUrl(reader.GetString(6));
            var hotelSlug = BuildHotelSlug(hotelName, hotelCode);

            if (string.IsNullOrWhiteSpace(hotelSlug) || roomTypeId <= 0)
            {
                continue;
            }

            var roomUrl = BuildAbsoluteUrl($"/hotel/{hotelSlug}?room={roomTypeId}");
            entries.Add(new SitemapUrlEntry
            {
                Location = roomUrl,
                LastModifiedUtc = DateTime.SpecifyKind(lastModifiedUtc, DateTimeKind.Utc),
                ChangeFrequency = "daily",
                Priority = 0.75m,
                ImageUrl = imageUrl,
                ImageTitle = $"{hotelName} - {roomName}",
                Images =
                [
                    new SitemapImageEntry
                    {
                        Url = imageUrl ?? string.Empty,
                        Title = roomName,
                        Caption = $"{roomName} · {price:N0} TRY/gece · {hotelName}"
                    }
                ]
            });
        }

        return entries;
    }

    private async Task<List<SitemapUrlEntry>> GetCampaignEntriesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                COALESCE(NULLIF(k.[SAYFA_URL], ''), CONCAT('/kampanyalar/', k.[SEO_SLUG])) AS relative_url,
                k.[KAMPANYA_ADI],
                COALESCE(k.[GUNCELLENME_TARIHI], k.[OLUSTURULMA_TARIHI], SYSUTCDATETIME()) AS last_modified,
                COALESCE(NULLIF(k.[KART_GORSELI], ''), NULLIF(k.[HERO_GORSELI], ''), NULLIF(k.[BANNER_GORSELI], '')) AS image_url
            FROM [dbo].[KAMPANYALAR] k
            WHERE k.[AKTIF_MI] = 1
              AND k.[GORUNURLUK_DURUMU] = N'Yayında'
              AND SYSUTCDATETIME() BETWEEN k.[BASLANGIC_TARIHI] AND k.[BITIS_TARIHI]
              AND COALESCE(NULLIF(k.[SEO_SLUG], ''), NULLIF(k.[SAYFA_URL], '')) <> ''
            ORDER BY k.id DESC;
            """;

        var entries = new List<SitemapUrlEntry>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var relativeUrl = reader.GetString(0);
            var name = NormalizeTurkishText(reader.IsDBNull(1) ? string.Empty : reader.GetString(1));
            var lastModifiedUtc = reader.GetDateTime(2);
            var imageUrl = reader.IsDBNull(3) ? null : NormalizeImageUrl(reader.GetString(3));

            entries.Add(new SitemapUrlEntry
            {
                Location = BuildAbsoluteUrl(relativeUrl),
                LastModifiedUtc = DateTime.SpecifyKind(lastModifiedUtc, DateTimeKind.Utc),
                ChangeFrequency = "daily",
                Priority = 0.7m,
                ImageUrl = imageUrl,
                ImageTitle = name
            });
        }

        return entries;
    }

    private async Task<List<SitemapUrlEntry>> GetBlogEntriesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                [SEO_SLUG],
                [BASLIK],
                COALESCE([HERO_GORSEL_URL], '') AS hero_image,
                COALESCE([GUNCELLENME_TARIHI], [OLUSTURULMA_TARIHI], SYSUTCDATETIME()) AS last_modified
            FROM [dbo].[YARDIM_MERKEZI_ICERIKLER]
            WHERE [AKTIF_MI] = 1
              AND [ICERIK_TURU] = N'blog'
              AND COALESCE(NULLIF([SEO_SLUG], ''), '') <> ''
            ORDER BY [ONE_CIKAN_MI] DESC, [SIRALAMA] ASC, [ID] DESC;
            """;

        var entries = new List<SitemapUrlEntry>();
        try
        {
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var slug = reader.GetString(0).Trim();
                if (string.IsNullOrWhiteSpace(slug))
                {
                    continue;
                }

                var title = NormalizeTurkishText(reader.IsDBNull(1) ? string.Empty : reader.GetString(1));
                var imageUrl = reader.IsDBNull(2) ? null : NormalizeImageUrl(reader.GetString(2));
                var lastModifiedUtc = reader.GetDateTime(3);

                entries.Add(new SitemapUrlEntry
                {
                    Location = BuildAbsoluteUrl("/blog/" + Uri.EscapeDataString(slug)),
                    LastModifiedUtc = DateTime.SpecifyKind(lastModifiedUtc, DateTimeKind.Utc),
                    ChangeFrequency = "weekly",
                    Priority = 0.55m,
                    ImageUrl = imageUrl,
                    ImageTitle = title
                });
            }
        }
        catch (SqlException)
        {
            // tablo yoksa blog sitemap bos kalir
        }

        return entries;
    }

    private async Task<List<SitemapUrlEntry>> GetLocationEntriesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                COALESCE(o.[SEHIR], '') AS sehir,
                COALESCE(o.[ILCE], '') AS ilce,
                MAX(COALESCE(o.[GUNCELLENME_TARIHI], o.[ONAY_TARIHI], o.[OLUSTURULMA_TARIHI], SYSUTCDATETIME())) AS last_modified
            FROM [dbo].[OTELLER] o
            WHERE o.[YAYIN_DURUMU] = N'Yayında'
              AND o.[ONAY_DURUMU] IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
              AND COALESCE(NULLIF(o.[SEHIR], ''), '') <> ''
            GROUP BY o.[SEHIR], o.[ILCE];
            """;

        var entries = new List<SitemapUrlEntry>();
        var citySeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var city = NormalizeTurkishText(reader.IsDBNull(0) ? string.Empty : reader.GetString(0));
            var district = NormalizeTurkishText(reader.IsDBNull(1) ? string.Empty : reader.GetString(1));
            var lastModifiedUtc = reader.GetDateTime(2);
            var citySlug = BuildAreaSlug(city);

            if (!string.IsNullOrWhiteSpace(citySlug) && citySeen.Add(citySlug))
            {
                entries.Add(new SitemapUrlEntry
                {
                    Location = BuildAbsoluteUrl("/hotel/" + citySlug),
                    LastModifiedUtc = DateTime.SpecifyKind(lastModifiedUtc, DateTimeKind.Utc),
                    ChangeFrequency = "daily",
                    Priority = 0.8m
                });
            }

            if (!string.IsNullOrWhiteSpace(district))
            {
                entries.Add(new SitemapUrlEntry
                {
                    Location = BuildAbsoluteUrl("/hotel?q=" + Uri.EscapeDataString(district)),
                    LastModifiedUtc = DateTime.SpecifyKind(lastModifiedUtc, DateTimeKind.Utc),
                    ChangeFrequency = "daily",
                    Priority = 0.65m
                });
            }
        }

        return entries;
    }

    private async Task<string> BuildHotelOffersFeedJsonAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                o.[OTEL_KODU],
                o.[OTEL_ADI],
                COALESCE(o.[SEHIR], '') AS sehir,
                COALESCE(o.[ILCE], '') AS ilce,
                COALESCE(o.[ENLEM], 0) AS latitude,
                COALESCE(o.[BOYLAM], 0) AS longitude,
                COALESCE(NULLIF(o.[KAPAK_FOTOGRAFI], ''), NULLIF(og.[GORSEL_URL], '')) AS image_url,
                ot.[ID] AS room_type_id,
                ot.[ODA_ADI],
                ot.[STANDART_GECELIK_FIYAT],
                COALESCE(ot.[GUNCELLENME_TARIHI], ot.[OLUSTURULMA_TARIHI], SYSUTCDATETIME()) AS last_modified
            FROM [dbo].[ODA_TIPLERI] ot
            INNER JOIN [dbo].[OTELLER] o ON o.[ID] = ot.[OTEL_ID]
            LEFT JOIN (
                SELECT g1.[OTEL_ID], g1.[GORSEL_URL]
                FROM (
                    SELECT
                        g.[OTEL_ID],
                        g.[GORSEL_URL],
                        ROW_NUMBER() OVER (PARTITION BY g.[OTEL_ID] ORDER BY g.[KAPAK_FOTOGRAFI_MI] DESC, g.[ONE_CIKAN] DESC, g.[SIRALAMA] ASC, g.id ASC) AS rn
                    FROM [dbo].[OTEL_GORSELLERI] g
                    WHERE g.[ONAY_DURUMU] IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
                      AND g.[GORSEL_URL] NOT LIKE '/uploads/logo/%'
                ) g1
                WHERE g1.rn = 1
            ) og ON og.[OTEL_ID] = o.id
            WHERE o.[YAYIN_DURUMU] = N'Yayında'
              AND o.[ONAY_DURUMU] IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
              AND ot.[AKTIF_MI] = 1
              AND ot.[STANDART_GECELIK_FIYAT] > 0
            ORDER BY o.id DESC, ot.[SIRALAMA] ASC, ot.id ASC;
            """;

        var hotels = new Dictionary<string, HotelOfferFeedHotel>(StringComparer.OrdinalIgnoreCase);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var hotelCode = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            var hotelName = NormalizeTurkishText(reader.IsDBNull(1) ? string.Empty : reader.GetString(1));
            var city = NormalizeTurkishText(reader.IsDBNull(2) ? string.Empty : reader.GetString(2));
            var district = NormalizeTurkishText(reader.IsDBNull(3) ? string.Empty : reader.GetString(3));
            var latitude = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4);
            var longitude = reader.IsDBNull(5) ? 0m : reader.GetDecimal(5);
            var imageUrl = reader.IsDBNull(6) ? null : NormalizeImageUrl(reader.GetString(6));
            var roomTypeId = reader.GetInt64(7);
            var roomName = NormalizeTurkishText(reader.IsDBNull(8) ? string.Empty : reader.GetString(8));
            var price = reader.GetDecimal(9);
            var lastModifiedUtc = reader.GetDateTime(10);
            var hotelSlug = BuildHotelSlug(hotelName, hotelCode);

            if (string.IsNullOrWhiteSpace(hotelSlug))
            {
                continue;
            }

            if (!hotels.TryGetValue(hotelSlug, out var hotel))
            {
                hotel = new HotelOfferFeedHotel
                {
                    Name = hotelName,
                    Url = BuildAbsoluteUrl("/hotel/" + hotelSlug),
                    City = city,
                    District = district,
                    ImageUrl = imageUrl,
                    Latitude = latitude,
                    Longitude = longitude
                };
                hotels[hotelSlug] = hotel;
            }

            hotel.Offers.Add(new HotelOfferFeedOffer
            {
                RoomTypeId = roomTypeId,
                RoomName = roomName,
                Price = price,
                Currency = "TRY",
                Url = BuildAbsoluteUrl($"/hotel/{hotelSlug}?room={roomTypeId}"),
                LastModifiedUtc = DateTime.SpecifyKind(lastModifiedUtc, DateTimeKind.Utc)
            });
        }

        var feed = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "DataFeed",
            ["name"] = "Otelturizm Hotel Offers Feed",
            ["dateModified"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture),
            ["dataFeedElement"] = hotels.Values.Select(hotel =>
            {
                var minPrice = hotel.Offers.Min(static x => x.Price);
                var maxPrice = hotel.Offers.Max(static x => x.Price);
                return new Dictionary<string, object?>
                {
                    ["@type"] = new[] { "Hotel", "LodgingBusiness" },
                    ["name"] = hotel.Name,
                    ["url"] = hotel.Url,
                    ["image"] = hotel.ImageUrl,
                    ["priceRange"] = $"{minPrice:N0}-{maxPrice:N0} TRY",
                    ["address"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "PostalAddress",
                        ["addressLocality"] = hotel.District,
                        ["addressRegion"] = hotel.City,
                        ["addressCountry"] = "TR"
                    },
                    ["geo"] = hotel.Latitude != 0m && hotel.Longitude != 0m
                        ? new Dictionary<string, object?>
                        {
                            ["@type"] = "GeoCoordinates",
                            ["latitude"] = hotel.Latitude,
                            ["longitude"] = hotel.Longitude
                        }
                        : null,
                    ["makesOffer"] = hotel.Offers.Select(offer => new Dictionary<string, object?>
                    {
                        ["@type"] = "Offer",
                        ["name"] = offer.RoomName,
                        ["price"] = offer.Price,
                        ["priceCurrency"] = offer.Currency,
                        ["url"] = offer.Url,
                        ["availability"] = "https://schema.org/InStock",
                        ["validFrom"] = offer.LastModifiedUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        ["itemOffered"] = new Dictionary<string, object?>
                        {
                            ["@type"] = "HotelRoom",
                            ["name"] = offer.RoomName,
                            ["occupancy"] = new Dictionary<string, object?>
                            {
                                ["@type"] = "QuantitativeValue",
                                ["unitText"] = "guests"
                            }
                        }
                    }).ToList()
                };
            }).ToList()
        };

        return JsonSerializer.Serialize(feed, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        });
    }

    private async Task SaveSubSitemapsAsync(IReadOnlyDictionary<string, string> files, CancellationToken cancellationToken)
    {
        var directoryPath = GetSubSitemapDirectoryPath();
        Directory.CreateDirectory(directoryPath);

        foreach (var oldPath in Directory.EnumerateFiles(directoryPath, "*.xml", SearchOption.TopDirectoryOnly))
        {
            File.Delete(oldPath);
        }

        foreach (var file in files)
        {
            var targetPath = Path.Combine(directoryPath, file.Key);
            await AtomicFileWriter.WriteFileAtomicAsync(targetPath, async (stream, ct) =>
            {
                var bytes = new UTF8Encoding(false).GetBytes(file.Value);
                await stream.WriteAsync(bytes, ct);
            }, cancellationToken);
        }
    }

    private async Task SaveRegionalSitemapsAsync(IReadOnlyCollection<RegionalSitemapFile> files, CancellationToken cancellationToken)
    {
        var directoryPath = GetRegionalSitemapDirectoryPath();
        Directory.CreateDirectory(directoryPath);

        foreach (var oldPath in Directory.EnumerateFiles(directoryPath, "*.xml", SearchOption.TopDirectoryOnly))
        {
            File.Delete(oldPath);
        }

        foreach (var file in files)
        {
            var targetPath = Path.Combine(directoryPath, file.FileName);
            await File.WriteAllTextAsync(targetPath, file.XmlContent, new UTF8Encoding(false), cancellationToken);
        }
    }

    private async Task SavePriceFeedAsync(string json, CancellationToken cancellationToken)
    {
        var path = GetPriceFeedFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await AtomicFileWriter.WriteFileAtomicAsync(path, async (stream, ct) =>
        {
            var bytes = new UTF8Encoding(false).GetBytes(json);
            await stream.WriteAsync(bytes, ct);
        }, cancellationToken);
    }

    private string GetSeoOutputRootPath()
        => Path.Combine(_environment.ContentRootPath, "App_Data", "seo");

    private string GetSitemapFilePath()
        => Path.Combine(GetSeoOutputRootPath(), "sitemap.xml");

    private string GetSubSitemapDirectoryPath()
        => Path.Combine(GetSeoOutputRootPath(), "sitemaps");

    private string GetPriceFeedFilePath()
        => Path.Combine(GetSeoOutputRootPath(), "feeds", "hotel-offers.json");

    private string GetRegionalSitemapDirectoryPath()
        => Path.Combine(_environment.ContentRootPath, "Views", "xml");

    private string GetSitemapLockFilePath()
        => Path.Combine(Path.GetDirectoryName(GetSitemapFilePath())!, ".sitemap.lock");

    private static async Task<FileStream?> TryAcquireFileLockAsync(string absoluteLockPath, bool force, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(absoluteLockPath)!);
        var attempts = force ? 8 : 1;
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            try
            {
                return new FileStream(absoluteLockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch when (force && attempt < attempts - 1)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250 * (attempt + 1)), cancellationToken);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static DateTime ResolveLastModifiedUtc(IReadOnlyCollection<SitemapUrlEntry> entries, DateTime fallbackUtc)
        => entries.Count == 0
            ? fallbackUtc
            : entries.Max(x => x.LastModifiedUtc ?? DateTime.MinValue);

    private string BuildAbsoluteUrl(string relativeOrAbsolute)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolute))
        {
            return BuildAbsoluteUrl("/");
        }

        if (Uri.TryCreate(relativeOrAbsolute, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        var baseUrl = _configuration["App:PublicBaseUrl"]?.TrimEnd('/') ?? "https://otelturizm.com";
        if (_environment.IsProduction()
            && baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = "https://otelturizm.com";
        }
        if (!relativeOrAbsolute.StartsWith('/'))
        {
            relativeOrAbsolute = "/" + relativeOrAbsolute;
        }

        return baseUrl + relativeOrAbsolute;
    }

    private string? NormalizeImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        return BuildAbsoluteUrl(imageUrl);
    }

    private static bool IsSafeXmlSlug(string fileName)
        => fileName.All(ch => char.IsLower(ch) || char.IsDigit(ch) || ch == '-');

    private static int CountXmlEntries(string path)
    {
        try
        {
            var document = XDocument.Load(path);
            var rootName = document.Root?.Name.LocalName ?? string.Empty;
            if (rootName.Equals("sitemapindex", StringComparison.OrdinalIgnoreCase))
            {
                return document.Descendants().Count(static x => x.Name.LocalName.Equals("sitemap", StringComparison.OrdinalIgnoreCase));
            }

            return document.Descendants().Count(static x => x.Name.LocalName.Equals("url", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return 0;
        }
    }

    private static int CountUrlEntriesFromXml(string xml)
    {
        try
        {
            var document = XDocument.Parse(xml);
            return document.Descendants().Count(static x => x.Name.LocalName.Equals("url", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return 0;
        }
    }

    private static int CountPriceFeedOffers(string path)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (!document.RootElement.TryGetProperty("dataFeedElement", out var elements) || elements.ValueKind != JsonValueKind.Array)
            {
                return 0;
            }

            var count = 0;
            foreach (var hotel in elements.EnumerateArray())
            {
                if (hotel.TryGetProperty("makesOffer", out var offers) && offers.ValueKind == JsonValueKind.Array)
                {
                    count += offers.GetArrayLength();
                }
            }

            return count;
        }
        catch
        {
            return 0;
        }
    }

    private static string BuildLocalizedUrl(string absoluteUrl, string locale)
    {
        if (!Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var uri))
        {
            return absoluteUrl;
        }

        if (uri.AbsolutePath.StartsWith("/hotel", StringComparison.OrdinalIgnoreCase)
            && LocaleToCultureCode(locale) is { } cultureCode)
        {
            var localizedPath = InternationalSeoPaths.LocalizePath(uri.AbsolutePath, cultureCode);
            var builder = new UriBuilder(uri) { Path = localizedPath };
            return builder.Uri.GetLeftPart(UriPartial.Path) + (string.IsNullOrEmpty(uri.Query) ? string.Empty : uri.Query);
        }

        var uriBuilder = new UriBuilder(uri);
        var qs = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uriBuilder.Query ?? string.Empty);
        var items = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in qs)
        {
            items[kv.Key] = kv.Value.Count > 0 ? kv.Value[0] : null;
        }

        items["lang"] = locale;
        uriBuilder.Query = string.Join("&", items
            .Where(static x => !string.IsNullOrWhiteSpace(x.Key))
            .Select(static x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value ?? string.Empty)}"));
        return uriBuilder.Uri.ToString();
    }

    private static string? LocaleToCultureCode(string locale) => locale switch
    {
        "tr-TR" => "tr",
        "en-US" or "en-GB" => "en",
        "de-DE" => "de",
        "fr-FR" => "fr",
        "es-ES" => "es",
        "ru-RU" => "ru",
        _ => null
    };

    private static string NormalizeTurkishText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var text = value.Trim();
        if (!LooksLikeMojibake(text))
        {
            return text;
        }

        try
        {
            var latinBytes = Encoding.GetEncoding(1252).GetBytes(text);
            var utf8Text = Encoding.UTF8.GetString(latinBytes);
            return string.IsNullOrWhiteSpace(utf8Text) ? text : utf8Text;
        }
        catch
        {
            return text;
        }
    }

    private static bool LooksLikeMojibake(string value)
        => value.Contains('Ã')
           || value.Contains('Å')
           || value.Contains('Ä')
           || value.Contains('Ð')
           || value.Contains('Þ');

    private static string BuildAreaSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return BuildHotelSlug(value, string.Empty);
    }

    private static string BuildHotelSlug(string hotelName, string hotelCode)
    {
        var source = string.IsNullOrWhiteSpace(hotelName) ? hotelCode : hotelName;
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        source = source
            .Trim()
            .Replace("İ", "I", StringComparison.Ordinal)
            .Replace("I", "i", StringComparison.Ordinal)
            .Replace("ı", "i", StringComparison.Ordinal)
            .Replace("Ğ", "G", StringComparison.Ordinal)
            .Replace("ğ", "g", StringComparison.Ordinal)
            .Replace("Ü", "U", StringComparison.Ordinal)
            .Replace("ü", "u", StringComparison.Ordinal)
            .Replace("Ş", "S", StringComparison.Ordinal)
            .Replace("ş", "s", StringComparison.Ordinal)
            .Replace("Ö", "O", StringComparison.Ordinal)
            .Replace("ö", "o", StringComparison.Ordinal)
            .Replace("Ç", "C", StringComparison.Ordinal)
            .Replace("ç", "c", StringComparison.Ordinal);

        var chars = new List<char>(source.Length);
        foreach (var ch in source.ToLowerInvariant())
        {
            chars.Add(ch switch
            {
                'ı' => 'i',
                'ğ' => 'g',
                'ü' => 'u',
                'ş' => 's',
                'ö' => 'o',
                'ç' => 'c',
                _ when char.IsLetterOrDigit(ch) => ch,
                _ => '-'
            });
        }

        var slug = new string(chars.ToArray()).Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(slug)
            ? (hotelCode ?? string.Empty).Trim().ToLowerInvariant()
            : slug;
    }

    private sealed record SitemapIndexEntry(string RelativePath, DateTime LastModifiedUtc, int UrlCount);

    private sealed record RegionalHotelRow(string City, string District, string HotelUrl, DateTime LastModifiedUtc);

    private sealed record RegionalSitemapFile(string FileName, string XmlContent, string PublicUrl);

    private sealed class HotelOfferFeedHotel
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public List<HotelOfferFeedOffer> Offers { get; set; } = new();
    }

    private sealed class HotelOfferFeedOffer
    {
        public long RoomTypeId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "TRY";
        public string Url { get; set; } = string.Empty;
        public DateTime LastModifiedUtc { get; set; }
    }
}
