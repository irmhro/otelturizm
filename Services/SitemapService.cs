using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;
using otelturizmnew.Models.Seo;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class SitemapService : ISitemapService
{
    private static readonly SemaphoreSlim SyncLock = new(1, 1);
    private static readonly string[] SitemapLocales = ["tr-TR", "en-US"];
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromDays(3);

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

            var regionalFiles = new List<RegionalSitemapFile>();
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
                regionalFiles = await BuildRegionalSitemapsAsync(connection, cancellationToken);
            }

            await SaveRegionalSitemapsAsync(regionalFiles, cancellationToken);

            var xml = await BuildSitemapXmlAsync(
                regionalFiles.Select(static file => file.PublicUrl).ToList(),
                cancellationToken);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, xml, new UTF8Encoding(false), cancellationToken);
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

        return await BuildSitemapXmlAsync([], cancellationToken);
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

    public async Task<SitemapDiagnosticsViewModel> GetDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureFreshSitemapAsync(false, cancellationToken);

        var model = new SitemapDiagnosticsViewModel
        {
            MainSitemapPhysicalPath = GetSitemapFilePath()
        };

        if (File.Exists(model.MainSitemapPhysicalPath))
        {
            model.LastRefreshUtc = File.GetLastWriteTimeUtc(model.MainSitemapPhysicalPath);
            model.MainSitemapUrlCount = CountUrlEntries(model.MainSitemapPhysicalPath);
            model.Files.Add(new SitemapFileSummaryViewModel
            {
                FileName = Path.GetFileName(model.MainSitemapPhysicalPath),
                PhysicalPath = model.MainSitemapPhysicalPath,
                PublicUrl = BuildAbsoluteUrl("/sitemap.xml"),
                ScopeText = "Ana sitemap",
                LastModifiedUtc = model.LastRefreshUtc,
                UrlCount = model.MainSitemapUrlCount
            });
        }

        var regionalDirectory = GetRegionalSitemapDirectoryPath();
        if (Directory.Exists(regionalDirectory))
        {
            var regionalFiles = Directory
                .EnumerateFiles(regionalDirectory, "*.xml", SearchOption.TopDirectoryOnly)
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var regionalPath in regionalFiles)
            {
                var fileName = Path.GetFileName(regionalPath);
                var slug = Path.GetFileNameWithoutExtension(regionalPath);
                if (!IsSafeXmlSlug(slug))
                {
                    continue;
                }

                var urlCount = CountUrlEntries(regionalPath);
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

        model.RegionalFileCount = model.Files.Count(static x => !x.FileName.Equals("sitemap.xml", StringComparison.OrdinalIgnoreCase));
        model.RegionalUrlCount = model.Files
            .Where(static x => !x.FileName.Equals("sitemap.xml", StringComparison.OrdinalIgnoreCase))
            .Sum(static x => x.UrlCount);

        return model;
    }

    private async Task<string> BuildSitemapXmlAsync(
        IReadOnlyCollection<string> regionalSitemapUrls,
        CancellationToken cancellationToken)
    {
        var entries = new List<SitemapUrlEntry>();
        entries.AddRange(BuildStaticEntries());

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            entries.AddRange(await GetHotelEntriesAsync(connection, cancellationToken));
            entries.AddRange(await GetCampaignEntriesAsync(connection, cancellationToken));
        }

        var now = DateTime.UtcNow;
        foreach (var regionalUrl in regionalSitemapUrls.Where(static x => !string.IsNullOrWhiteSpace(x)))
        {
            entries.Add(new SitemapUrlEntry
            {
                Location = regionalUrl,
                LastModifiedUtc = now,
                ChangeFrequency = "weekly",
                Priority = 0.5m
            });
        }

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
                    .Select(x => x
                        .OrderByDescending(y => y.LastModifiedUtc ?? DateTime.MinValue)
                        .First())
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

                        if (!string.IsNullOrWhiteSpace(entry.ImageUrl))
                        {
                            var imageElement = new XElement(imageNs + "image",
                                new XElement(imageNs + "loc", entry.ImageUrl));

                            if (!string.IsNullOrWhiteSpace(entry.ImageTitle))
                            {
                                imageElement.Add(new XElement(imageNs + "title", entry.ImageTitle));
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
                o.otel_adi,
                o.otel_kodu,
                COALESCE(o.sehir, '') AS sehir,
                COALESCE(o.ilce, '') AS ilce,
                COALESCE(o.guncellenme_tarihi, o.onay_tarihi, o.olusturulma_tarihi, SYSUTCDATETIME()) AS last_modified
            FROM oteller o
            WHERE o.yayin_durumu = N'Yayında'
              AND o.onay_durumu IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
              AND COALESCE(NULLIF(o.otel_adi, ''), NULLIF(o.otel_kodu, '')) <> '';
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
                    BuildAbsoluteUrl("/oteller/" + slug),
                    DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc)));
            }
        }

        var result = new List<RegionalSitemapFile>();
        var byCity = hotels
            .Where(static x => !string.IsNullOrWhiteSpace(x.City))
            .GroupBy(x => x.City, StringComparer.OrdinalIgnoreCase);

        foreach (var cityGroup in byCity)
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

            var byDistrict = cityGroup
                .Where(static x => !string.IsNullOrWhiteSpace(x.District))
                .GroupBy(x => x.District, StringComparer.OrdinalIgnoreCase);

            foreach (var districtGroup in byDistrict)
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

    private static string BuildLocalizedUrl(string absoluteUrl, string locale)
    {
        if (!Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var uri))
        {
            return absoluteUrl;
        }

        var builder = new UriBuilder(uri);
        var qs = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(builder.Query ?? string.Empty);
        var items = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in qs)
        {
            items[kv.Key] = kv.Value.Count > 0 ? kv.Value[0] : null;
        }
        items["lang"] = locale;
        builder.Query = string.Join("&", items
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value ?? string.Empty)}"));
        return builder.Uri.ToString();
    }

    private IEnumerable<SitemapUrlEntry> BuildStaticEntries()
    {
        var now = DateTime.UtcNow;
        return
        [
            CreateStatic("/", "daily", 1.0m, now),
            CreateStatic("/oteller", "daily", 0.9m, now),
            CreateStatic("/kampanyalar", "daily", 0.8m, now),
            CreateStatic("/kurumsal", "weekly", 0.7m, now),
            CreateStatic("/firma", "weekly", 0.7m, now),
            CreateStatic("/yardim-merkezi", "weekly", 0.6m, now),
            CreateStatic("/sss", "weekly", 0.6m, now),
            CreateStatic("/Home/Privacy", "monthly", 0.3m, now)
        ];
    }

    private SitemapUrlEntry CreateStatic(string relativePath, string changeFrequency, decimal priority, DateTime lastModifiedUtc)
    {
        return new SitemapUrlEntry
        {
            Location = BuildAbsoluteUrl(relativePath),
            ChangeFrequency = changeFrequency,
            Priority = priority,
            LastModifiedUtc = lastModifiedUtc
        };
    }

    private async Task<List<SitemapUrlEntry>> GetHotelEntriesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                o.otel_kodu,
                o.otel_adi,
                COALESCE(o.guncellenme_tarihi, o.onay_tarihi, o.olusturulma_tarihi, SYSUTCDATETIME()) AS last_modified,
                COALESCE(NULLIF(o.kapak_fotografi, ''), NULLIF(og.gorsel_url, '')) AS image_url
            FROM oteller o
            LEFT JOIN (
                SELECT g1.otel_id, g1.gorsel_url
                FROM (
                    SELECT
                        g.otel_id,
                        g.gorsel_url,
                        ROW_NUMBER() OVER (PARTITION BY g.otel_id ORDER BY g.kapak_fotografi_mi DESC, g.one_cikan DESC, g.siralama ASC, g.id ASC) AS rn
                    FROM otel_gorselleri g
                    WHERE g.onay_durumu IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
                      AND g.gorsel_url NOT LIKE '/uploads/logo/%'
                ) g1
                WHERE g1.rn = 1
            ) og ON og.otel_id = o.id
            WHERE o.yayin_durumu = N'Yayında'
              AND o.onay_durumu IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
              AND COALESCE(NULLIF(o.otel_adi, ''), NULLIF(o.otel_kodu, '')) <> ''
            ORDER BY o.id DESC;
            """;

        var entries = new List<SitemapUrlEntry>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var hotelCode = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            var name = NormalizeTurkishText(reader.IsDBNull(1) ? string.Empty : reader.GetString(1));
            var lastModifiedUtc = reader.GetDateTime(2);
            var imageUrl = reader.IsDBNull(3) ? null : NormalizeImageUrl(reader.GetString(3));
            var slug = BuildHotelSlug(name, hotelCode);

            if (string.IsNullOrWhiteSpace(slug))
            {
                continue;
            }

            entries.Add(new SitemapUrlEntry
            {
                Location = BuildAbsoluteUrl("/oteller/" + slug),
                LastModifiedUtc = DateTime.SpecifyKind(lastModifiedUtc, DateTimeKind.Utc),
                ChangeFrequency = "daily",
                Priority = 0.8m,
                ImageUrl = imageUrl,
                ImageTitle = name
            });
        }

        return entries;
    }

    private async Task<List<SitemapUrlEntry>> GetCampaignEntriesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                COALESCE(NULLIF(k.sayfa_url, ''), CONCAT('/kampanyalar/', k.seo_slug)) AS relative_url,
                k.kampanya_adi,
                COALESCE(k.guncellenme_tarihi, k.olusturulma_tarihi, SYSUTCDATETIME()) AS last_modified,
                COALESCE(NULLIF(k.kart_gorseli, ''), NULLIF(k.hero_gorseli, ''), NULLIF(k.banner_gorseli, '')) AS image_url
            FROM kampanyalar k
            WHERE k.aktif_mi = 1
              AND k.gorunurluk_durumu = N'Yayında'
              AND SYSUTCDATETIME() BETWEEN k.baslangic_tarihi AND k.bitis_tarihi
              AND COALESCE(NULLIF(k.seo_slug, ''), NULLIF(k.sayfa_url, '')) <> ''
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

    private string GetSitemapFilePath()
    {
        var webRootPath = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        return Path.Combine(webRootPath, "sitemap.xml");
    }

    private string GetRegionalSitemapDirectoryPath()
    {
        return Path.Combine(_environment.ContentRootPath, "Views", "xml");
    }

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
    {
        return fileName.All(ch => char.IsLower(ch) || char.IsDigit(ch) || ch == '-');
    }

    private static int CountUrlEntries(string path)
    {
        try
        {
            var document = XDocument.Load(path);
            return document
                .Descendants()
                .Count(x => x.Name.LocalName.Equals("url", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return 0;
        }
    }

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

        var source = NormalizeTurkishText(value).Trim();
        source = source
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
                _ when char.IsLetterOrDigit(ch) => ch,
                _ => '-'
            });
        }

        var slug = new string(chars.ToArray()).Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug;
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

    private sealed record RegionalHotelRow(
        string City,
        string District,
        string HotelUrl,
        DateTime LastModifiedUtc);

    private sealed record RegionalSitemapFile(
        string FileName,
        string XmlContent,
        string PublicUrl);
}
