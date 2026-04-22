using System.Text;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly IWebHostEnvironment _environment;

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

        var normalized = relativeViewPath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        var absolutePath = Path.Combine(_environment.ContentRootPath, normalized);
        if (!File.Exists(absolutePath))
        {
            return RenderFallbackTemplate(relativeViewPath, tokens, absolutePath);
        }

        var content = await File.ReadAllTextAsync(absolutePath, Encoding.UTF8, cancellationToken);
        foreach (var token in tokens)
        {
            var key = token.Key.StartsWith("{{", StringComparison.Ordinal) ? token.Key : $"{{{{{token.Key}}}}}";
            content = content.Replace(key, token.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return content;
    }

    private static string RenderFallbackTemplate(string relativeViewPath, IReadOnlyDictionary<string, string> tokens, string absolutePath)
    {
        var normalized = relativeViewPath.Replace('\\', '/');
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
