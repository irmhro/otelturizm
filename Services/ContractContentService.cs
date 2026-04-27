
using System.Globalization;
using Microsoft.Data.SqlClient;
using otelturizmnew.Models.Email;
using otelturizmnew.Models.Legal;
using otelturizmnew.Models.Paneller.Admin;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class ContractContentService : IContractContentService
{
    private readonly string _connectionString;
    private readonly string _publicBaseUrl;
    private readonly IEmailQueueService _emailQueueService;

    public ContractContentService(IConfiguration configuration, IEmailQueueService emailQueueService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _publicBaseUrl = (configuration["App:PublicBaseUrl"] ?? "https://localhost:7223").TrimEnd('/');
        _emailQueueService = emailQueueService;
    }

    public async Task<ContractDetailPageViewModel?> GetPublicContractBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return await LoadContractDetailAsync(connection, null, slug.Trim().ToLowerInvariant(), cancellationToken);
    }

    public async Task<IReadOnlyList<ContractLinkViewModel>> GetActiveContractsForAudienceAsync(string audience, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return await LoadContractLinksAsync(connection, null, NormalizeAudience(audience), cancellationToken);
    }

    public async Task RecordRegistrationAcceptancesAsync(SqlConnection connection, SqlTransaction? transaction, ContractAcceptanceRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        var audience = NormalizeAudience(request.Audience);
        var wantedTypes = new List<string>();
        if (request.IncludePrimaryAgreement)
        {
            wantedTypes.Add("agreement");
        }

        if (request.IncludeKvkk)
        {
            wantedTypes.Add("kvkk");
        }

        if (wantedTypes.Count == 0)
        {
            return;
        }

        const string sql = @"
            SELECT id, baslik, versiyon_no, sozlesme_tipi
            FROM sozlesmeler
            WHERE hedef_kitle = @audience
              AND sozlesme_tipi IN ('agreement', 'kvkk')
              AND aktif_mi = 1
              AND kabul_gerektirir_mi = 1
              AND baslangic_tarihi <= SYSUTCDATETIME()
              AND (bitis_tarihi IS NULL OR bitis_tarihi >= SYSUTCDATETIME())
            ORDER BY CASE sozlesme_tipi WHEN 'agreement' THEN 0 ELSE 1 END, id ASC;";

        var contracts = new List<(long Id, string Title, int VersionNo, string Type)>();
        await using (var command = new SqlCommand(sql, connection, transaction))
        {
            command.Parameters.AddWithValue("@audience", audience);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                contracts.Add((
                    reader.GetInt64(0),
                    reader.GetString(1),
                reader.IsDBNull(2) ? 1 : Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture),
                    reader.GetString(3)));
            }
        }

        foreach (var contract in contracts.Where(c => wantedTypes.Contains(c.Type, StringComparer.OrdinalIgnoreCase)))
        {
            const string insertSql = @"
                INSERT INTO sozlesme_kabulleri
                (
                    sozlesme_id, kabul_eden_tip, kullanici_id, partner_id, firma_id, alici_eposta,
                    sozlesme_baslik_snapshot, sozlesme_versiyon_snapshot, kabul_kaynagi, kabul_ip,
                    kabul_user_agent, eposta_dogrulandi_mi, kabul_tarihi, durum
                )
                VALUES
                (
                    @contractId, @audience, @userId, @partnerId, @firmaId, @email,
                    @title, @versionNo, @source, @ipAddress,
                    @userAgent, 0, SYSUTCDATETIME(), 'KabulEdildi'
                );";

            await using var insertCommand = new SqlCommand(insertSql, connection, transaction);
            insertCommand.Parameters.AddWithValue("@contractId", contract.Id);
            insertCommand.Parameters.AddWithValue("@audience", audience);
            insertCommand.Parameters.AddWithValue("@userId", request.UserId);
            insertCommand.Parameters.AddWithValue("@partnerId", (object?)request.PartnerId ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@firmaId", (object?)request.FirmaId ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@email", request.Email);
            insertCommand.Parameters.AddWithValue("@title", contract.Title);
            insertCommand.Parameters.AddWithValue("@versionNo", contract.VersionNo);
            insertCommand.Parameters.AddWithValue("@source", request.Source);
            insertCommand.Parameters.AddWithValue("@ipAddress", (object?)TrimOrNull(request.IpAddress, 80) ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@userAgent", (object?)TrimOrNull(request.UserAgent, 500) ?? DBNull.Value);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task FinalizeEmailVerificationAsync(SqlConnection connection, SqlTransaction? transaction, long userId, string email, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var recipient = await ResolveRecipientAsync(connection, transaction, userId, email, cancellationToken);
        if (recipient is null)
        {
            return;
        }

        await using (var updateCommand = new SqlCommand(@"
            UPDATE sozlesme_kabulleri
            SET eposta_dogrulandi_mi = 1,
                eposta_dogrulama_tarihi = COALESCE(eposta_dogrulama_tarihi, SYSUTCDATETIME())
            WHERE kullanici_id = @userId
              AND alici_eposta = @email
              AND eposta_dogrulandi_mi = 0;", connection, transaction))
        {
            updateCommand.Parameters.AddWithValue("@userId", userId);
            updateCommand.Parameters.AddWithValue("@email", recipient.Email);
            await updateCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        var contracts = await LoadEmailBundleContractsAsync(connection, transaction, recipient.Audience, cancellationToken);
        if (contracts.Count == 0)
        {
            return;
        }

        var sectionsHtml = string.Join(Environment.NewLine, contracts.Select(static contract =>
            $"<section style=\"padding:16px 0;border-bottom:1px solid #e5e7eb;\"><h3 style=\"margin:0 0 8px;font-size:18px;\">{contract.Title}</h3><p style=\"margin:0 0 10px;color:#475569;\">{contract.Subtitle}</p><p style=\"margin:0 0 8px;\"><a href=\"{contract.Url}\" style=\"color:#0f4aa3;font-weight:700;text-decoration:none;\">Sözleşmeyi görüntüle</a></p><div style=\"color:#64748b;font-size:13px;\">Versiyon: {contract.VersionText}</div></section>"));

        var attachments = await LoadContractPdfAttachmentsAsync(connection, transaction, contracts.Select(static x => x.ContractId).ToList(), cancellationToken);

        await _emailQueueService.QueueTemplateAsync(
            connection,
            transaction,
            new QueuedEmailTemplateRequest
            {
                UserId = recipient.UserId,
                RecipientEmail = recipient.Email,
                TemplateCode = "contract_delivery",
                RelatedTable = "users",
                RelatedRecordId = recipient.UserId,
                Attachments = attachments,
                Tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["recipient_name"] = recipient.FullName,
                    ["module_label"] = MapAudienceLabel(recipient.Audience),
                    ["contract_bundle_title"] = $"{MapAudienceLabel(recipient.Audience)} sözleşme ve KVKK paketiniz",
                    ["contract_sections_html"] = sectionsHtml,
                    ["primary_contract_url"] = contracts[0].Url
                }
            },
            cancellationToken);
        foreach (var contract in contracts)
        {
            const string logSql = @"
                INSERT INTO sozlesme_gonderim_loglari
                (
                    sozlesme_id, kullanici_id, partner_id, firma_id, alici_eposta, gonderim_nedeni,
                    konu_snapshot, icerik_snapshot, durum, gonderim_tarihi, ip_adresi, user_agent
                )
                VALUES
                (
                    @contractId, @userId, @partnerId, @firmaId, @email, 'EmailDogrulamaSonrasi',
                    @subject, @body, 'KuyrugaAlindi', SYSUTCDATETIME(), @ipAddress, @userAgent
                );";

            await using var logCommand = new SqlCommand(logSql, connection, transaction);
            logCommand.Parameters.AddWithValue("@contractId", contract.ContractId);
            logCommand.Parameters.AddWithValue("@userId", recipient.UserId);
            logCommand.Parameters.AddWithValue("@partnerId", (object?)recipient.PartnerId ?? DBNull.Value);
            logCommand.Parameters.AddWithValue("@firmaId", (object?)recipient.FirmaId ?? DBNull.Value);
            logCommand.Parameters.AddWithValue("@email", recipient.Email);
            logCommand.Parameters.AddWithValue("@subject", $"{MapAudienceLabel(recipient.Audience)} sözleşme ve KVKK bilgilendirmeniz");
            logCommand.Parameters.AddWithValue("@body", sectionsHtml);
            logCommand.Parameters.AddWithValue("@ipAddress", (object?)TrimOrNull(ipAddress, 80) ?? DBNull.Value);
            logCommand.Parameters.AddWithValue("@userAgent", (object?)TrimOrNull(userAgent, 500) ?? DBNull.Value);
            await logCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<AdminContractManagementPageViewModel> GetAdminContractManagementAsync(string fullName, string email, string userRole, long? contractId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var model = new AdminContractManagementPageViewModel
        {
            Shell = await LoadAdminShellAsync(connection, fullName, email, userRole, cancellationToken)
        };

        model.Shell.PanelTitle = "Sözleşmeler";
        model.Shell.PanelSubtitle = "Kullanıcı, partner ve firma sözleşmelerini HTML içerik olarak yönetin.";

        model.SummaryCards.AddRange(new[]
        {
            new AdminSummaryCardViewModel { Label = "Aktif Sözleşme", Description = "Yayında olan sürümler", ToneClass = "success", IconClass = "fa-file-signature" },
            new AdminSummaryCardViewModel { Label = "Toplam Kabul", Description = "Kayıt esnasında alınan onay", ToneClass = "info", IconClass = "fa-check-double" },
            new AdminSummaryCardViewModel { Label = "Doğrulanmış Kabul", Description = "E-posta doğrulaması tamamlanan onay", ToneClass = "warning", IconClass = "fa-envelope-circle-check" },
            new AdminSummaryCardViewModel { Label = "E-posta Gönderimi", Description = "Sözleşme paket kuyruk kayıtları", ToneClass = "danger", IconClass = "fa-paper-plane" }
        });

        var summarySqls = new[]
        {
            "SELECT COUNT(*) FROM sozlesmeler WHERE aktif_mi = 1;",
            "SELECT COUNT(*) FROM sozlesme_kabulleri;",
            "SELECT COUNT(*) FROM sozlesme_kabulleri WHERE eposta_dogrulandi_mi = 1;",
            "SELECT COUNT(*) FROM sozlesme_gonderim_loglari;"
        };

        for (var i = 0; i < summarySqls.Length; i++)
        {
            await using var summaryCommand = new SqlCommand(summarySqls[i], connection);
            model.SummaryCards[i].Value = Convert.ToString(await summaryCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) ?? "0";
        }

        const string listSql = @"
            SELECT s.id, s.hedef_kitle, s.sozlesme_tipi, s.baslik, s.slug, s.versiyon_no,
                   CONCAT(FORMAT(s.baslangic_tarihi, 'dd.MM.yyyy', 'tr-TR'),
                          CASE WHEN s.bitis_tarihi IS NULL THEN ' - Süresiz' ELSE CONCAT(' - ', FORMAT(s.bitis_tarihi, 'dd.MM.yyyy', 'tr-TR')) END),
                   (SELECT COUNT(*) FROM sozlesme_kabulleri sk WHERE sk.sozlesme_id = s.id),
                   (SELECT COUNT(*) FROM sozlesme_gonderim_loglari sg WHERE sg.sozlesme_id = s.id),
                   s.aktif_mi
            FROM sozlesmeler s
            ORDER BY s.hedef_kitle, s.sozlesme_tipi, s.versiyon_no DESC, s.id DESC;";

        await using (var listCommand = new SqlCommand(listSql, connection))
        await using (var reader = await listCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                model.Contracts.Add(new AdminContractRowViewModel
                {
                    ContractId = reader.GetInt64(0),
                    Audience = reader.GetString(1),
                    ContractType = reader.GetString(2),
                    Title = reader.GetString(3),
                    Slug = reader.GetString(4),
                VersionText = $"v{Convert.ToInt32(reader.GetValue(5), CultureInfo.InvariantCulture)}",
                    EffectiveRangeText = reader.GetString(6),
                    AcceptanceText = Convert.ToString(reader.GetValue(7), CultureInfo.InvariantCulture) ?? "0",
                    DeliveryText = Convert.ToString(reader.GetValue(8), CultureInfo.InvariantCulture) ?? "0",
                    IsActive = !reader.IsDBNull(9) && reader.GetBoolean(9)
                });
            }
        }

        model.Form = contractId.HasValue
            ? await LoadAdminContractFormAsync(connection, contractId.Value, cancellationToken) ?? new AdminContractForm()
            : new AdminContractForm();

        return model;
    }

    public async Task<(bool Success, string Message)> SaveContractAsync(long adminUserId, AdminContractForm request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Slug) || string.IsNullOrWhiteSpace(request.ContentHtml))
        {
            return (false, "Başlık, slug ve içerik zorunludur.");
        }

        var audience = NormalizeAudience(request.Audience);
        var contractType = NormalizeContractType(request.ContractType);
        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            long savedId;
            if (request.ContractId.HasValue && request.ContractId.Value > 0)
            {
                const string updateSql = @"
                    UPDATE sozlesmeler
                    SET hedef_kitle = @audience,
                        sozlesme_tipi = @contractType,
                        baslik = @title,
                        alt_baslik = @subtitle,
                        slug = @slug,
                        ozet_html = @summaryHtml,
                        icerik_html = @contentHtml,
                        gorsel_url = @heroImageUrl,
                        sozlesme_linki = @contractUrl,
                        versiyon_no = @versionNo,
                        baslangic_tarihi = @effectiveStart,
                        bitis_tarihi = @effectiveEnd,
                        kabul_gerektirir_mi = @requiresAcceptance,
                        email_dogrulamada_gonder = @sendOnVerification,
                        aktif_mi = @isActive,
                        notlar = @note,
                        guncellenme_tarihi = SYSUTCDATETIME(),
                        guncelleyen_kullanici_id = @adminUserId
                    WHERE id = @contractId;";

                await using var updateCommand = new SqlCommand(updateSql, connection, transaction);
                BindContractForm(updateCommand, request, adminUserId, audience, contractType, normalizedSlug);
                updateCommand.Parameters.AddWithValue("@contractId", request.ContractId.Value);
                await updateCommand.ExecuteNonQueryAsync(cancellationToken);
                savedId = request.ContractId.Value;
            }
            else
            {
                const string insertSql = @"
                    INSERT INTO sozlesmeler
                    (
                        hedef_kitle, sozlesme_tipi, baslik, alt_baslik, slug, ozet_html, icerik_html, gorsel_url, sozlesme_linki,
                        versiyon_no, baslangic_tarihi, bitis_tarihi, kabul_gerektirir_mi, email_dogrulamada_gonder,
                        aktif_mi, notlar, olusturan_kullanici_id, olusturulma_tarihi, guncellenme_tarihi
                    )
                    VALUES
                    (
                        @audience, @contractType, @title, @subtitle, @slug, @summaryHtml, @contentHtml, @heroImageUrl, @contractUrl,
                        @versionNo, @effectiveStart, @effectiveEnd, @requiresAcceptance, @sendOnVerification,
                        @isActive, @note, @adminUserId, SYSUTCDATETIME(), SYSUTCDATETIME()
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS bigint);";

                await using var insertCommand = new SqlCommand(insertSql, connection, transaction);
                BindContractForm(insertCommand, request, adminUserId, audience, contractType, normalizedSlug);
                savedId = Convert.ToInt64(await insertCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            }

            if (request.ForceResendAfterSave)
            {
                await ResendContractBundleInternalAsync(connection, transaction, adminUserId, savedId, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, "Sözleşme kaydı güncellendi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Sözleşme kaydedilirken hata oluştu: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Message)> ResendContractBundleAsync(long adminUserId, long contractId, CancellationToken cancellationToken = default)
    {
        if (contractId <= 0)
        {
            return (false, "Gönderilecek sözleşme bulunamadı.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var deliveryCount = await ResendContractBundleInternalAsync(connection, transaction, adminUserId, contractId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (true, $"Sözleşme güncellemesi {deliveryCount} alıcı için e-posta kuyruğuna alındı.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Sözleşme gönderimi sırasında hata oluştu: {ex.Message}");
        }
    }

    public async Task<(string Title, string Html)?> GetAdminContractPreviewAsync(long contractId, CancellationToken cancellationToken = default)
    {
        if (contractId <= 0)
        {
            return null;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT TOP (1) baslik, COALESCE(ozet_html, ''), COALESCE(icerik_html, '')
            FROM sozlesmeler
            WHERE id = @contractId
            ORDER BY id DESC;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@contractId", contractId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var title = reader.IsDBNull(0) ? "Sözleşme" : reader.GetString(0);
        var summaryHtml = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
        var contentHtml = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);

        var html = string.IsNullOrWhiteSpace(summaryHtml)
            ? contentHtml
            : $"<div class=\"legal-contract-summary\">{summaryHtml}</div>{contentHtml}";

        return (title, html);
    }

    private async Task<int> ResendContractBundleInternalAsync(SqlConnection connection, SqlTransaction? transaction, long adminUserId, long contractId, CancellationToken cancellationToken)
    {
        const string contractSql = @"
            SELECT TOP (1) hedef_kitle
            FROM sozlesmeler
            WHERE id = @contractId;";

        string audience;
        await using (var contractCommand = new SqlCommand(contractSql, connection, transaction))
        {
            contractCommand.Parameters.AddWithValue("@contractId", contractId);
            var scalar = await contractCommand.ExecuteScalarAsync(cancellationToken);
            if (scalar is null || scalar == DBNull.Value)
            {
                throw new InvalidOperationException("Sözleşme kaydı bulunamadı.");
            }

            audience = Convert.ToString(scalar, CultureInfo.InvariantCulture) ?? "user";
        }

        var recipients = new List<ContractRecipient>();
        const string recipientSql = @"
            SELECT DISTINCT u.id, u.ad_soyad, u.eposta, sk.partner_id, sk.firma_id
            FROM sozlesme_kabulleri sk
            INNER JOIN users u ON u.id = sk.kullanici_id
            WHERE sk.sozlesme_id = @contractId
              AND u.email_dogrulama_tarihi IS NOT NULL
              AND u.hesap_durumu = 1;";

        await using (var recipientCommand = new SqlCommand(recipientSql, connection, transaction))
        {
            recipientCommand.Parameters.AddWithValue("@contractId", contractId);
            await using var reader = await recipientCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                recipients.Add(new ContractRecipient
                {
                    UserId = reader.GetInt64(0),
                    FullName = reader.GetString(1),
                    Email = reader.GetString(2),
                    PartnerId = reader.IsDBNull(3) ? null : reader.GetInt64(3),
                    FirmaId = reader.IsDBNull(4) ? null : reader.GetInt64(4),
                    Audience = audience
                });
            }
        }

        var contracts = await LoadEmailBundleContractsAsync(connection, transaction, audience, cancellationToken);
        if (contracts.Count == 0)
        {
            return 0;
        }

        var sectionsHtml = string.Join(Environment.NewLine, contracts.Select(static contract =>
            $"<section style=\"padding:16px 0;border-bottom:1px solid #e5e7eb;\"><h3 style=\"margin:0 0 8px;font-size:18px;\">{contract.Title}</h3><p style=\"margin:0 0 10px;color:#475569;\">{contract.Subtitle}</p><p style=\"margin:0 0 8px;\"><a href=\"{contract.Url}\" style=\"color:#0f4aa3;font-weight:700;text-decoration:none;\">Sözleşmeyi görüntüle</a></p><div style=\"color:#64748b;font-size:13px;\">Versiyon: {contract.VersionText}</div></section>"));

        var attachments = await LoadContractPdfAttachmentsAsync(connection, transaction, contracts.Select(static x => x.ContractId).ToList(), cancellationToken);

        var count = 0;
        foreach (var recipient in recipients)
        {
            await _emailQueueService.QueueTemplateAsync(
                connection,
                transaction,
                new QueuedEmailTemplateRequest
                {
                    UserId = recipient.UserId,
                    RecipientEmail = recipient.Email,
                    TemplateCode = "contract_delivery",
                    RelatedTable = "sozlesmeler",
                    RelatedRecordId = contractId,
                    Attachments = attachments,
                    Tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["recipient_name"] = recipient.FullName,
                        ["module_label"] = MapAudienceLabel(audience),
                        ["contract_bundle_title"] = $"{MapAudienceLabel(audience)} sözleşme güncellemesi",
                        ["contract_sections_html"] = sectionsHtml,
                        ["primary_contract_url"] = contracts[0].Url
                    }
                },
                cancellationToken);

            foreach (var contract in contracts)
            {
                await using var logCommand = new SqlCommand(@"
                    INSERT INTO sozlesme_gonderim_loglari
                    (
                        sozlesme_id, kullanici_id, partner_id, firma_id, alici_eposta, gonderim_nedeni,
                        konu_snapshot, icerik_snapshot, durum, gonderim_tarihi, olusturan_admin_id
                    )
                    VALUES
                    (
                        @contractId, @userId, @partnerId, @firmaId, @email, 'AdminYenidenGonderim',
                        @subject, @body, 'KuyrugaAlindi', SYSUTCDATETIME(), @adminUserId
                    );", connection, transaction);
                logCommand.Parameters.AddWithValue("@contractId", contract.ContractId);
                logCommand.Parameters.AddWithValue("@userId", recipient.UserId);
                logCommand.Parameters.AddWithValue("@partnerId", (object?)recipient.PartnerId ?? DBNull.Value);
                logCommand.Parameters.AddWithValue("@firmaId", (object?)recipient.FirmaId ?? DBNull.Value);
                logCommand.Parameters.AddWithValue("@email", recipient.Email);
                logCommand.Parameters.AddWithValue("@subject", $"{MapAudienceLabel(audience)} sözleşme güncellemesi");
                logCommand.Parameters.AddWithValue("@body", sectionsHtml);
                logCommand.Parameters.AddWithValue("@adminUserId", adminUserId);
                await logCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            count++;
        }

        return count;
    }

    private async Task<List<QueuedEmailAttachment>?> LoadContractPdfAttachmentsAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        IReadOnlyList<long> contractIds,
        CancellationToken cancellationToken)
    {
        if (contractIds.Count == 0)
        {
            return null;
        }

        // Migration uygulanmamış olabilir.
        const string sql = @"
            IF OBJECT_ID('dbo.sozlesme_dosyalari', 'U') IS NULL
            BEGIN
                SELECT CAST(NULL AS bigint) AS sozlesme_id, CAST(NULL AS nvarchar(500)) AS dosya_yolu, CAST(NULL AS nvarchar(250)) AS dosya_adi
                WHERE 1 = 0;
                RETURN;
            END

            SELECT s.sozlesme_id, s.dosya_yolu, COALESCE(s.dosya_adi, '') AS dosya_adi
            FROM sozlesme_dosyalari s
            INNER JOIN (
                SELECT sozlesme_id, MAX(id) AS max_id
                FROM sozlesme_dosyalari
                WHERE dosya_tipi = 'pdf'
                  AND sozlesme_id IN (SELECT value FROM STRING_SPLIT(@ids, ','))
                GROUP BY sozlesme_id
            ) x ON x.sozlesme_id = s.sozlesme_id AND x.max_id = s.id;";

        var idCsv = string.Join(",", contractIds.Distinct());
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@ids", idCsv);

        var attachments = new List<QueuedEmailAttachment>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var contractId = reader.IsDBNull(0) ? 0 : reader.GetInt64(0);
            var fileUrl = reader.IsDBNull(1) ? null : reader.GetString(1);
            var fileName = reader.IsDBNull(2) ? null : reader.GetString(2);
            if (contractId <= 0 || string.IsNullOrWhiteSpace(fileUrl))
            {
                continue;
            }

            // dosya_yolu; URL (/uploads/... veya http...) veya fiziksel path olabilir.
            var urlOrPath = Path.IsPathRooted(fileUrl)
                ? fileUrl
                : fileUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? fileUrl
                    : $"{_publicBaseUrl}{fileUrl}";

            var safeFileName = string.IsNullOrWhiteSpace(fileName)
                ? $"sozlesme-{contractId}.pdf"
                : (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? fileName : $"{fileName}.pdf");

            attachments.Add(new QueuedEmailAttachment
            {
                FileName = safeFileName,
                FilePathOrUrl = urlOrPath,
                ContentType = "application/pdf"
            });
        }

        return attachments.Count == 0 ? null : attachments;
    }

    private async Task<ContractDetailPageViewModel?> LoadContractDetailAsync(SqlConnection connection, SqlTransaction? transaction, string slug, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1) id, hedef_kitle, sozlesme_tipi, baslik, alt_baslik, ozet_html, icerik_html,
                   gorsel_url, versiyon_no, baslangic_tarihi
            FROM sozlesmeler
            WHERE slug = @slug
              AND aktif_mi = 1
              AND baslangic_tarihi <= SYSUTCDATETIME()
              AND (bitis_tarihi IS NULL OR bitis_tarihi >= SYSUTCDATETIME())
            ORDER BY versiyon_no DESC, id DESC;";

        ContractDetailPageViewModel? model = null;
        string audience;
        await using (var command = new SqlCommand(sql, connection, transaction))
        {
            command.Parameters.AddWithValue("@slug", slug);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            audience = reader.GetString(1);
            model = new ContractDetailPageViewModel
            {
                ContractId = reader.GetInt64(0),
                Audience = audience,
                ContractType = reader.GetString(2),
                Title = reader.GetString(3),
                Subtitle = reader.IsDBNull(4) ? null : reader.GetString(4),
                SummaryHtml = reader.IsDBNull(5) ? null : reader.GetString(5),
                ContentHtml = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                HeroImageUrl = reader.IsDBNull(7) ? null : reader.GetString(7),
                VersionText = $"Versiyon {Convert.ToInt32(reader.GetValue(8), CultureInfo.InvariantCulture)}",
                EffectiveDateText = reader.GetDateTime(9).ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("tr-TR"))
            };
        }

        model.RelatedContracts = (await LoadContractLinksAsync(connection, transaction, audience, cancellationToken)).ToList();
        return model;
    }

    private async Task<IReadOnlyList<ContractLinkViewModel>> LoadContractLinksAsync(SqlConnection connection, SqlTransaction? transaction, string audience, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, baslik, slug, sozlesme_tipi
            FROM sozlesmeler
            WHERE hedef_kitle = @audience
              AND aktif_mi = 1
              AND baslangic_tarihi <= SYSUTCDATETIME()
              AND (bitis_tarihi IS NULL OR bitis_tarihi >= SYSUTCDATETIME())
            ORDER BY CASE sozlesme_tipi WHEN 'agreement' THEN 0 WHEN 'kvkk' THEN 1 ELSE 2 END, versiyon_no DESC, id DESC;";

        var items = new List<ContractLinkViewModel>();
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@audience", audience);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new ContractLinkViewModel
            {
                ContractId = reader.GetInt64(0),
                Title = reader.GetString(1),
                Slug = reader.GetString(2),
                ContractType = reader.GetString(3)
            });
        }

        return items;
    }
    private async Task<List<ContractEmailBundleRow>> LoadEmailBundleContractsAsync(SqlConnection connection, SqlTransaction? transaction, string audience, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, baslik, COALESCE(alt_baslik, ''), slug, versiyon_no
            FROM sozlesmeler
            WHERE hedef_kitle = @audience
              AND aktif_mi = 1
              AND email_dogrulamada_gonder = 1
              AND baslangic_tarihi <= SYSUTCDATETIME()
              AND (bitis_tarihi IS NULL OR bitis_tarihi >= SYSUTCDATETIME())
            ORDER BY CASE sozlesme_tipi WHEN 'agreement' THEN 0 WHEN 'kvkk' THEN 1 ELSE 2 END, versiyon_no DESC, id DESC;";

        var rows = new List<ContractEmailBundleRow>();
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@audience", audience);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new ContractEmailBundleRow
            {
                ContractId = reader.GetInt64(0),
                Title = reader.GetString(1),
                Subtitle = reader.GetString(2),
                Url = $"{_publicBaseUrl}/sozlesmeler/{reader.GetString(3)}",
                VersionText = $"v{Convert.ToInt32(reader.GetValue(4), CultureInfo.InvariantCulture)}"
            });
        }

        return rows;
    }

    private async Task<ContractRecipient?> ResolveRecipientAsync(SqlConnection connection, SqlTransaction? transaction, long userId, string email, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1)
                u.id,
                u.ad_soyad,
                u.eposta,
                u.rol,
                u.firma_id,
                p.id AS partner_id
            FROM users u
            LEFT JOIN partner_detaylari p ON p.kullanici_id = u.id
            WHERE u.id = @userId;";

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var role = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
        long? firmaId = reader.IsDBNull(4) ? null : reader.GetInt64(4);
        long? partnerId = reader.IsDBNull(5) ? null : reader.GetInt64(5);
        var audience = partnerId.HasValue || role.StartsWith("partner", StringComparison.OrdinalIgnoreCase)
            ? "partner"
            : firmaId.HasValue || role.StartsWith("firma", StringComparison.OrdinalIgnoreCase)
                ? "firma"
                : "user";

        return new ContractRecipient
        {
            UserId = reader.GetInt64(0),
            FullName = reader.GetString(1),
            Email = string.IsNullOrWhiteSpace(email) ? reader.GetString(2) : email,
            Audience = audience,
            PartnerId = partnerId,
            FirmaId = firmaId
        };
    }

    private async Task<AdminContractForm?> LoadAdminContractFormAsync(SqlConnection connection, long contractId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1)
                id, hedef_kitle, sozlesme_tipi, baslik, alt_baslik, slug, ozet_html, icerik_html, gorsel_url,
                sozlesme_linki, versiyon_no, baslangic_tarihi, bitis_tarihi,
                kabul_gerektirir_mi, email_dogrulamada_gonder, aktif_mi, notlar
            FROM sozlesmeler
            WHERE id = @contractId;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@contractId", contractId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new AdminContractForm
        {
            ContractId = reader.GetInt64(0),
            Audience = reader.GetString(1),
            ContractType = reader.GetString(2),
            Title = reader.GetString(3),
            Subtitle = reader.IsDBNull(4) ? null : reader.GetString(4),
            Slug = reader.GetString(5),
            SummaryHtml = reader.IsDBNull(6) ? null : reader.GetString(6),
            ContentHtml = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            HeroImageUrl = reader.IsDBNull(8) ? null : reader.GetString(8),
            ContractUrl = reader.IsDBNull(9) ? null : reader.GetString(9),
            VersionNo = Convert.ToInt32(reader.GetValue(10), CultureInfo.InvariantCulture),
            EffectiveStartDate = reader.GetDateTime(11),
            EffectiveEndDate = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
            RequiresAcceptance = !reader.IsDBNull(13) && reader.GetBoolean(13),
            SendOnEmailVerification = !reader.IsDBNull(14) && reader.GetBoolean(14),
            IsActive = !reader.IsDBNull(15) && reader.GetBoolean(15),
            Note = reader.IsDBNull(16) ? null : reader.GetString(16)
        };
    }

    private async Task<AdminShellViewModel> LoadAdminShellAsync(SqlConnection connection, string fullName, string email, string userRole, CancellationToken cancellationToken)
    {
        var shell = new AdminShellViewModel
        {
            FullName = fullName,
            Email = email,
            UserRole = userRole,
            PanelTitle = "Sözleşmeler",
            PanelSubtitle = "Sözleşme ve KVKK içeriklerini yönetin."
        };

        const string shellSql = @"
            SELECT
                (SELECT COUNT(*) FROM partner_detaylari WHERE onay_durumu = 'Beklemede') AS pending_partner,
                (SELECT COUNT(*) FROM bildirim_loglari WHERE durum = 'Beklemede') AS unread_notifications,
                (SELECT COUNT(*) FROM sistem_hata_loglari WHERE hata_seviyesi IN ('Error', 'Critical')) AS critical_logs,
                (SELECT COUNT(*) FROM yorumlar WHERE onay_durumu = 'Beklemede') AS pending_reviews;";

        await using var command = new SqlCommand(shellSql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            shell.PendingPartnerApplications = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture);
            shell.UnreadNotifications = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture);
            shell.CriticalLogs = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture);
            shell.PendingReviews = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3), CultureInfo.InvariantCulture);
        }

        return shell;
    }

    private static void BindContractForm(SqlCommand command, AdminContractForm request, long adminUserId, string audience, string contractType, string normalizedSlug)
    {
        command.Parameters.AddWithValue("@audience", audience);
        command.Parameters.AddWithValue("@contractType", contractType);
        command.Parameters.AddWithValue("@title", request.Title.Trim());
        command.Parameters.AddWithValue("@subtitle", (object?)TrimOrNull(request.Subtitle, 250) ?? DBNull.Value);
        command.Parameters.AddWithValue("@slug", normalizedSlug);
        command.Parameters.AddWithValue("@summaryHtml", (object?)request.SummaryHtml?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@contentHtml", request.ContentHtml.Trim());
        command.Parameters.AddWithValue("@heroImageUrl", (object?)TrimOrNull(request.HeroImageUrl, 500) ?? DBNull.Value);
        command.Parameters.AddWithValue("@contractUrl", (object?)TrimOrNull(request.ContractUrl, 500) ?? DBNull.Value);
        command.Parameters.AddWithValue("@versionNo", Math.Max(1, request.VersionNo));
        command.Parameters.AddWithValue("@effectiveStart", request.EffectiveStartDate.Date);
        command.Parameters.AddWithValue("@effectiveEnd", request.EffectiveEndDate.HasValue ? request.EffectiveEndDate.Value.Date : DBNull.Value);
        command.Parameters.AddWithValue("@requiresAcceptance", request.RequiresAcceptance);
        command.Parameters.AddWithValue("@sendOnVerification", request.SendOnEmailVerification);
        command.Parameters.AddWithValue("@isActive", request.IsActive);
        command.Parameters.AddWithValue("@note", (object?)request.Note?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@adminUserId", adminUserId);
    }

    private static string NormalizeAudience(string audience)
    {
        return (audience ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "partner" => "partner",
            "firma" => "firma",
            _ => "user"
        };
    }

    private static string NormalizeContractType(string contractType)
    {
        return (contractType ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "kvkk" => "kvkk",
            _ => "agreement"
        };
    }
    private static string MapAudienceLabel(string audience)
    {
        return audience switch
        {
            "partner" => "Partner",
            "firma" => "Firma",
            _ => "Kullanıcı"
        };
    }

    private static string? TrimOrNull(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private sealed class ContractRecipient
    {
        public long UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Audience { get; set; } = "user";
        public long? PartnerId { get; set; }
        public long? FirmaId { get; set; }
    }

    private sealed class ContractEmailBundleRow
    {
        public long ContractId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string VersionText { get; set; } = string.Empty;
    }
}
