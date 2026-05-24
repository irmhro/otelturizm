using System.Text;
using System.Globalization;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class EmailTemplateService : IEmailTemplateService
{
    private const string EmailMasterRelativePath = "Views/Email/_EmailMaster.cshtml";
    private readonly IWebHostEnvironment _environment;
    private static readonly string[] SupportedLanguages = ["tr", "en", "de", "fr", "es", "ru"];

    public EmailTemplateService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> RenderTemplateFileAsync(string relativeViewPath, IReadOnlyDictionary<string, string> tokens, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativeViewPath))
        {
            throw new InvalidOperationException("E-posta şablon yolu boş.");
        }

        var lang = ResolveLanguage(tokens);
        var localizedViewPath = ResolveLocalizedViewPath(relativeViewPath, tokens);
        if (!TryResolveTemplateAbsolutePath(localizedViewPath, out var absolutePath))
        {
            foreach (var fallbackPath in BuildLocalizedFallbackPaths(relativeViewPath, lang))
            {
                if (TryResolveTemplateAbsolutePath(fallbackPath, out absolutePath))
                {
                    localizedViewPath = fallbackPath;
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
        {
            var fallbackViewPath = ResolveNeutralViewPath(localizedViewPath);
            if (!string.Equals(fallbackViewPath, localizedViewPath, StringComparison.OrdinalIgnoreCase)
                && TryResolveTemplateAbsolutePath(fallbackViewPath, out var neutralAbsolutePath)
                && !string.IsNullOrWhiteSpace(neutralAbsolutePath))
            {
                return await ReadAndApplyTokensAsync(neutralAbsolutePath, tokens, cancellationToken);
            }

            return RenderFallbackTemplate(localizedViewPath, tokens, absolutePath ?? localizedViewPath);
        }

        return await ReadAndApplyTokensAsync(absolutePath!, tokens, cancellationToken);
    }

    private async Task<string> ReadAndApplyTokensAsync(string absolutePath, IReadOnlyDictionary<string, string> tokens, CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(absolutePath, Encoding.UTF8, cancellationToken);
        content = await ComposeWithEmailMasterIfNeededAsync(content, absolutePath, tokens, cancellationToken);
        return ApplyTokens(content, tokens);
    }

    private static string ApplyTokens(string content, IReadOnlyDictionary<string, string> tokens)
    {
        foreach (var token in tokens)
        {
            var key = token.Key.StartsWith("{{", StringComparison.Ordinal) ? token.Key : $"{{{{{token.Key}}}}}";
            content = content.Replace(key, token.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return content;
    }

    private async Task<string> ComposeWithEmailMasterIfNeededAsync(
        string templateContent,
        string templateAbsolutePath,
        IReadOnlyDictionary<string, string> tokens,
        CancellationToken cancellationToken)
    {
        if (!TryParseEmailLayout(templateContent, out var layoutMeta, out var bodyContent))
        {
            return templateContent;
        }

        var masterAbsolutePath = Path.Combine(_environment.ContentRootPath, EmailMasterRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(masterAbsolutePath))
        {
            return templateContent;
        }

        var lang = ResolveLanguage(tokens);
        var master = await File.ReadAllTextAsync(masterAbsolutePath, Encoding.UTF8, cancellationToken);
        master = master.Replace("{{Body}}", bodyContent, StringComparison.Ordinal);

        var emailLang = layoutMeta.EmailLang ?? lang;
        var isRtl = string.Equals(emailLang, "ar", StringComparison.OrdinalIgnoreCase);

        master = master.Replace("{{email_lang}}", emailLang, StringComparison.OrdinalIgnoreCase);
        master = master.Replace("{{email_dir}}", isRtl ? "rtl" : "ltr", StringComparison.OrdinalIgnoreCase);
        master = master.Replace("{{email_rtl_class}}", isRtl ? "email-rtl" : string.Empty, StringComparison.OrdinalIgnoreCase);
        master = master.Replace("{{email_title}}", layoutMeta.EmailTitle ?? "Otelturizm", StringComparison.OrdinalIgnoreCase);
        master = master.Replace("{{email_preheader}}", layoutMeta.Preheader ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        master = master.Replace("{{email_header_tagline}}", layoutMeta.HeaderTagline ?? "OTELTURIZM", StringComparison.OrdinalIgnoreCase);
        master = master.Replace("{{email_header_title}}", layoutMeta.HeaderTitle ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        master = master.Replace("{{email_header_subtitle}}", layoutMeta.HeaderSubtitle ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        master = master.Replace("{{email_footer_line1}}", layoutMeta.FooterLine1 ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        master = master.Replace("{{email_footer_line2}}", layoutMeta.FooterLine2 ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        master = master.Replace("{{email_footer_legal}}", layoutMeta.FooterLegal ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        master = master.Replace("{{email_footer_year}}", DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);

        return master;
    }

    private static bool TryParseEmailLayout(string templateContent, out EmailLayoutMeta meta, out string bodyContent)
    {
        meta = new EmailLayoutMeta();
        bodyContent = templateContent;

        if (string.IsNullOrWhiteSpace(templateContent)
            || !templateContent.Contains("Layout: _EmailMaster", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var lines = templateContent.Replace("\r\n", "\n").Split('\n');
        var bodyLines = new List<string>(lines.Length);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("@*", StringComparison.Ordinal) && trimmed.EndsWith("*@", StringComparison.Ordinal))
            {
                var inner = trimmed[2..^2].Trim();
                if (inner.StartsWith("Layout:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var colonIndex = inner.IndexOf(':');
                if (colonIndex > 0)
                {
                    var key = inner[..colonIndex].Trim();
                    var value = inner[(colonIndex + 1)..].Trim();
                    switch (key.ToLowerInvariant())
                    {
                        case "emailtitle":
                            meta.EmailTitle = value;
                            break;
                        case "preheader":
                            meta.Preheader = value;
                            break;
                        case "headertagline":
                            meta.HeaderTagline = value;
                            break;
                        case "headertitle":
                            meta.HeaderTitle = value;
                            break;
                        case "headersubtitle":
                            meta.HeaderSubtitle = value;
                            break;
                        case "footerline1":
                            meta.FooterLine1 = value;
                            break;
                        case "footerline2":
                            meta.FooterLine2 = value;
                            break;
                        case "footerlegal":
                            meta.FooterLegal = value;
                            break;
                        case "emaillang":
                            meta.EmailLang = NormalizeLang(value);
                            break;
                    }
                }

                continue;
            }

            bodyLines.Add(line);
        }

        bodyContent = string.Join(Environment.NewLine, bodyLines).TrimStart();
        return true;
    }

    private bool TryResolveTemplateAbsolutePath(string relativeViewPath, out string? absolutePath)
    {
        absolutePath = null;
        var normalized = relativeViewPath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        var candidate = Path.Combine(_environment.ContentRootPath, normalized);

        var asciiViewPath = ToAsciiTurkish(relativeViewPath);
        var asciiCandidate = !string.Equals(asciiViewPath, relativeViewPath, StringComparison.Ordinal)
            ? Path.Combine(_environment.ContentRootPath, asciiViewPath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar))
            : candidate;

        if (File.Exists(candidate))
        {
            absolutePath = candidate;
            return true;
        }

        if (File.Exists(asciiCandidate))
        {
            absolutePath = asciiCandidate;
            return true;
        }

        var compactViewPath = RemoveFileNameSpaces(relativeViewPath);
        if (!string.Equals(compactViewPath, relativeViewPath, StringComparison.Ordinal))
        {
            var compactCandidate = Path.Combine(_environment.ContentRootPath, compactViewPath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar));
            if (File.Exists(compactCandidate))
            {
                absolutePath = compactCandidate;
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> BuildLocalizedFallbackPaths(string relativeViewPath, string lang)
    {
        var normalized = (relativeViewPath ?? string.Empty).Replace('\\', '/').Trim();
        if (!normalized.StartsWith("Views/Email/", StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        var fileName = normalized;
        foreach (var supported in SupportedLanguages)
        {
            var prefix = $"Views/Email/{supported}/";
            if (fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName[prefix.Length..];
                break;
            }
        }

        if (fileName.StartsWith("Views/Email/", StringComparison.OrdinalIgnoreCase))
        {
            fileName = fileName["Views/Email/".Length..];
        }

        var chain = new List<string> { lang, "en", "tr" };
        foreach (var fallbackLang in chain.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            yield return $"Views/Email/{fallbackLang}/{fileName}";
        }

        yield return $"Views/Email/{fileName}";
    }

    private sealed class EmailLayoutMeta
    {
        public string? EmailTitle { get; set; }
        public string? Preheader { get; set; }
        public string? HeaderTagline { get; set; }
        public string? HeaderTitle { get; set; }
        public string? HeaderSubtitle { get; set; }
        public string? FooterLine1 { get; set; }
        public string? FooterLine2 { get; set; }
        public string? FooterLegal { get; set; }
        public string? EmailLang { get; set; }
    }

    private static string ToAsciiTurkish(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return value
            .Replace('ı', 'i').Replace('İ', 'I')
            .Replace('ş', 's').Replace('Ş', 'S')
            .Replace('ğ', 'g').Replace('Ğ', 'G')
            .Replace('ü', 'u').Replace('Ü', 'U')
            .Replace('ö', 'o').Replace('Ö', 'O')
            .Replace('ç', 'c').Replace('Ç', 'C');
    }

    private static string RemoveFileNameSpaces(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var normalized = value.Replace('\\', '/');
        var slashIndex = normalized.LastIndexOf('/');
        if (slashIndex < 0)
        {
            return normalized.Replace(" ", string.Empty, StringComparison.Ordinal);
        }

        return normalized[..(slashIndex + 1)] + normalized[(slashIndex + 1)..].Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    private static string ResolveLocalizedViewPath(string relativeViewPath, IReadOnlyDictionary<string, string> tokens)
    {
        var normalized = (relativeViewPath ?? string.Empty).Replace('\\', '/').Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return relativeViewPath ?? string.Empty;
        }

        // If path already points to a locale subfolder, respect it.
        foreach (var supported in SupportedLanguages)
        {
            var segment = $"/Views/Email/{supported}/";
            if (normalized.Contains(segment, StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith($"Views/Email/{supported}/", StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }
        }

        // Only apply localization to Views/Email/*
        if (!normalized.StartsWith("Views/Email/", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        var lang = ResolveLanguage(tokens);
        if (string.IsNullOrWhiteSpace(lang) || !SupportedLanguages.Contains(lang, StringComparer.OrdinalIgnoreCase))
        {
            lang = "tr";
        }

        // candidate: Views/Email/{lang}/File.cshtml
        var fileName = normalized.Substring("Views/Email/".Length);
        return $"Views/Email/{lang}/{fileName}";
    }

    private static string ResolveNeutralViewPath(string relativeViewPath)
    {
        var normalized = (relativeViewPath ?? string.Empty).Replace('\\', '/').Trim();
        foreach (var supported in SupportedLanguages)
        {
            var prefix = $"Views/Email/{supported}/";
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return "Views/Email/" + normalized[prefix.Length..];
            }
        }

        return normalized;
    }

    private static string ResolveLanguage(IReadOnlyDictionary<string, string> tokens)
    {
        // Highest priority: explicit token
        if (tokens is not null)
        {
            if (tokens.TryGetValue("Language", out var language) && !string.IsNullOrWhiteSpace(language))
            {
                return NormalizeLang(language);
            }
            if (tokens.TryGetValue("lang", out var lang) && !string.IsNullOrWhiteSpace(lang))
            {
                return NormalizeLang(lang);
            }
            if (tokens.TryGetValue("culture", out var culture) && !string.IsNullOrWhiteSpace(culture))
            {
                return NormalizeLang(culture);
            }
            if (tokens.TryGetValue("locale", out var locale) && !string.IsNullOrWhiteSpace(locale))
            {
                return NormalizeLang(locale);
            }
        }

        return NormalizeLang(CultureInfo.CurrentUICulture?.Name ?? CultureInfo.CurrentCulture?.Name ?? "tr");
    }

    private static string NormalizeLang(string value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return "tr";
        }

        // Accept: en / en-US / EN_us / tr-TR ...
        var first = trimmed.Split('-', '_', ' ').FirstOrDefault() ?? trimmed;
        var normalized = first.ToLowerInvariant();
        return SupportedLanguages.Contains(normalized, StringComparer.Ordinal) ? normalized : "tr";
    }

    private static string RenderFallbackTemplate(string relativeViewPath, IReadOnlyDictionary<string, string> tokens, string absolutePath)
    {
        var normalized = (relativeViewPath ?? string.Empty).Replace('\\', '/');
        foreach (var supported in SupportedLanguages)
        {
            normalized = normalized
                .Replace($"/Views/Email/{supported}/", "/Views/Email/", StringComparison.OrdinalIgnoreCase)
                .Replace($"Views/Email/{supported}/", "Views/Email/", StringComparison.OrdinalIgnoreCase);
        }

        if (normalized.EndsWith("Views/Email/E-posta Adresini Onayla.cshtml", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith("Views/Email/E-posta Adresini Onayla.cshtml".Replace("ş", "s"), StringComparison.OrdinalIgnoreCase))
        {
            var firstName = ReadToken(tokens, "user_first_name", "Misafir");
            var email = ReadToken(tokens, "user_email");
            var registrationDate = ReadToken(tokens, "registration_date");
            var verificationLink = ReadToken(tokens, "verification_link", "https://otelturizm.com/eposta-dogrula");
            var verificationCode = ReadToken(tokens, "verification_code");

            return $$"""
                     <!DOCTYPE html>
                     <html lang="tr">
                     <head>
                         <meta charset="utf-8" />
                         <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                         <title>E-posta Adresinizi Onaylayın</title>
                     </head>
                     <body style="margin:0;padding:0;background:#f5f7fb;font-family:Arial,sans-serif;color:#10203a;">
                         <div style="max-width:620px;margin:0 auto;padding:32px 16px;">
                             <div style="background:#ffffff;border-radius:20px;padding:32px;border:1px solid #dbe4f0;">
                                 <p style="margin:0 0 12px;font-size:13px;font-weight:700;letter-spacing:.08em;color:#2f6fed;">OTELTURIZM</p>
                                 <h1 style="margin:0 0 16px;font-size:28px;line-height:1.2;color:#10203a;">E-posta adresinizi onaylayın</h1>
                                 <p style="margin:0 0 12px;font-size:16px;line-height:1.6;">Merhaba {{firstName}},</p>
                                 <p style="margin:0 0 12px;font-size:16px;line-height:1.6;">{{email}} adresiyle oluşturduğunuz hesabı tamamlamak için aşağıdaki kodu kullanabilir veya bağlantıya tıklayabilirsiniz.</p>
                                 <div style="margin:0 0 20px;padding:18px 24px;border-radius:16px;background:#edf4ff;border:1px solid #c7dbff;text-align:center;">
                                     <span style="display:block;font-size:34px;font-weight:800;letter-spacing:.18em;color:#174ea6;">{{verificationCode}}</span>
                                 </div>
                                 <p style="margin:0 0 16px;font-size:14px;line-height:1.6;color:#4d5f7a;">Kayıt zamanı: {{registrationDate}}</p>
                                 <p style="margin:0;">
                                     <a href="{{verificationLink}}" style="display:inline-block;padding:14px 22px;border-radius:999px;background:#174ea6;color:#ffffff;font-weight:700;text-decoration:none;">E-postamı doğrula</a>
                                 </p>
                             </div>
                         </div>
                     </body>
                     </html>
                     """;
        }

        if (normalized.EndsWith("Views/Email/Şifre Sıfırlama Talebi.cshtml", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith("Views/Email/Sifre Sifirlama Talebi.cshtml", StringComparison.OrdinalIgnoreCase))
        {
            var firstName = ReadToken(tokens, "user_first_name", "Misafir");
            var email = ReadToken(tokens, "user_email");
            var resetLink = ReadToken(tokens, "reset_link", "https://otelturizm.com/sifremi-unuttum");
            var requestIp = ReadToken(tokens, "request_ip", "-");

            return $$"""
                     <!DOCTYPE html>
                     <html lang="tr">
                     <head>
                         <meta charset="utf-8" />
                         <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                         <title>Şifre Sıfırlama Talebi</title>
                     </head>
                     <body style="margin:0;padding:0;background:#f5f7fb;font-family:Arial,sans-serif;color:#10203a;">
                         <div style="max-width:620px;margin:0 auto;padding:32px 16px;">
                             <div style="background:#ffffff;border-radius:20px;padding:32px;border:1px solid #dbe4f0;">
                                 <p style="margin:0 0 12px;font-size:13px;font-weight:700;letter-spacing:.08em;color:#2f6fed;">OTELTURIZM</p>
                                 <h1 style="margin:0 0 16px;font-size:28px;line-height:1.2;color:#10203a;">Şifre sıfırlama bağlantınız hazır</h1>
                                 <p style="margin:0 0 12px;font-size:16px;line-height:1.6;">Merhaba {{firstName}},</p>
                                 <p style="margin:0 0 12px;font-size:16px;line-height:1.6;">{{email}} hesabınız için bir şifre sıfırlama talebi aldık. İşlem size aitse aşağıdaki bağlantıyla yeni şifrenizi oluşturabilirsiniz.</p>
                                 <p style="margin:0 0 20px;">
                                     <a href="{{resetLink}}" style="display:inline-block;padding:14px 22px;border-radius:999px;background:#174ea6;color:#ffffff;font-weight:700;text-decoration:none;">Şifremi sıfırla</a>
                                 </p>
                                 <p style="margin:0;font-size:14px;line-height:1.6;color:#4d5f7a;">İstek IP adresi: {{requestIp}}. Bu işlemi siz yapmadıysanız bu e-postayı dikkate almayabilirsiniz.</p>
                             </div>
                         </div>
                     </body>
                     </html>
                     """;
        }

        if (normalized.EndsWith("Views/Email/Giris Guvenlik Kodu.cshtml", StringComparison.OrdinalIgnoreCase))
        {
            var code = ReadToken(tokens, "verification_code");
            var firstName = ReadToken(tokens, "user_first_name", "Misafir");
            var loginTime = ReadToken(tokens, "login_time");

            return $$"""
                     <!DOCTYPE html>
                     <html lang="tr">
                     <head>
                         <meta charset="utf-8" />
                         <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                         <title>Giriş Güvenlik Kodunuz</title>
                     </head>
                     <body style="margin:0;padding:0;background:#f5f7fb;font-family:Arial,sans-serif;color:#10203a;">
                         <div style="max-width:620px;margin:0 auto;padding:32px 16px;">
                             <div style="background:#ffffff;border-radius:20px;padding:32px;border:1px solid #dbe4f0;">
                                 <p style="margin:0 0 12px;font-size:13px;font-weight:700;letter-spacing:.08em;color:#2f6fed;">OTELTURIZM</p>
                                 <h1 style="margin:0 0 16px;font-size:28px;line-height:1.2;color:#10203a;">Giriş güvenlik kodunuz hazır</h1>
                                 <p style="margin:0 0 12px;font-size:16px;line-height:1.6;">Merhaba {{firstName}},</p>
                                 <p style="margin:0 0 20px;font-size:16px;line-height:1.6;">Hesabınıza giriş yapmak için aşağıdaki tek kullanımlık güvenlik kodunu kullanın.</p>
                                 <div style="margin:0 0 20px;padding:18px 24px;border-radius:16px;background:#edf4ff;border:1px solid #c7dbff;text-align:center;">
                                     <span style="display:block;font-size:34px;font-weight:800;letter-spacing:.28em;color:#174ea6;">{{code}}</span>
                                 </div>
                                 <p style="margin:0 0 8px;font-size:14px;line-height:1.6;color:#4d5f7a;">Kod oluşturulma zamanı: {{loginTime}}</p>
                                 <p style="margin:0;font-size:14px;line-height:1.6;color:#4d5f7a;">Bu işlemi siz yapmadıysanız hesabınızın şifresini değiştirin.</p>
                             </div>
                         </div>
                     </body>
                     </html>
                     """;
        }

        if (normalized.EndsWith("Views/Email/Sozlesme Bildirimi.cshtml", StringComparison.OrdinalIgnoreCase))
        {
            var recipientName = ReadToken(tokens, "recipient_name", "Misafir");
            var moduleLabel = ReadToken(tokens, "module_label", "Hesap");
            var title = ReadToken(tokens, "contract_bundle_title", $"{moduleLabel} sözleşme ve KVKK paketiniz");
            var sectionsHtml = ReadToken(tokens, "contract_sections_html");
            var primaryUrl = ReadToken(tokens, "primary_contract_url", "https://otelturizm.com");

            return $$"""
                     <!DOCTYPE html>
                     <html lang="tr">
                     <head>
                         <meta charset="utf-8" />
                         <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                         <title>{{title}}</title>
                     </head>
                     <body style="margin:0;padding:0;background:#f5f7fb;font-family:Arial,sans-serif;color:#10203a;">
                         <div style="max-width:680px;margin:0 auto;padding:32px 16px;">
                             <div style="background:#ffffff;border-radius:20px;padding:32px;border:1px solid #dbe4f0;">
                                 <p style="margin:0 0 12px;font-size:13px;font-weight:700;letter-spacing:.08em;color:#2f6fed;">OTELTURIZM</p>
                                 <h1 style="margin:0 0 16px;font-size:28px;line-height:1.2;color:#10203a;">{{title}}</h1>
                                 <p style="margin:0 0 12px;font-size:16px;line-height:1.6;">Merhaba {{recipientName}},</p>
                                 <p style="margin:0 0 20px;font-size:16px;line-height:1.6;">{{moduleLabel}} hesabınız için geçerli sözleşme ve KVKK içeriklerini aşağıda bulabilirsiniz.</p>
                                 <div style="margin:0 0 20px;">{{sectionsHtml}}</div>
                                 <p style="margin:0;font-size:14px;line-height:1.6;color:#4d5f7a;">
                                     Tüm içeriklere buradan da erişebilirsiniz:
                                     <a href="{{primaryUrl}}" style="color:#174ea6;font-weight:700;text-decoration:none;">Sözleşmeyi görüntüle</a>
                                 </p>
                             </div>
                         </div>
                     </body>
                     </html>
                     """;
        }

        throw new FileNotFoundException($"E-posta şablonu bulunamadı: {absolutePath}");
    }

    private static string ReadToken(IReadOnlyDictionary<string, string> tokens, string key, string fallback = "")
    {
        return tokens.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }
}
