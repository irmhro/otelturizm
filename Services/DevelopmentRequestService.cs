using System.Globalization;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using otelturizmnew.Models.Paneller.Admin;
using otelturizmnew.Models.Paneller.Developer;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class DevelopmentRequestService : IDevelopmentRequestService
{
    private readonly string _connectionString;

    public DevelopmentRequestService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
    }

    public async Task<DeveloperDashboardViewModel> GetDeveloperDashboardAsync(long currentUserId, string fullName, string email, string? searchText = null, string? statusFilter = null, CancellationToken cancellationToken = default)
    {
        var model = new DeveloperDashboardViewModel
        {
            Shell = new DeveloperShellViewModel
            {
                FullName = fullName,
                Email = email,
                PanelTitle = "Developer Paneli",
                PanelSubtitle = "Gelistirme taleplerini topla, cevaplari takip et ve teslimleri kontrole hazir hale getir."
            },
            SearchText = (searchText ?? string.Empty).Trim(),
            StatusFilter = NormalizeStatusFilter(statusFilter)
        };

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        model.Requests = await LoadRequestCardsAsync(connection, currentUserId, false, model.SearchText, model.StatusFilter, "all", null, cancellationToken);
        model.Shell.OpenRequestCount = model.Requests.Count(x => !IsClosedStatus(x.Status));
        model.Shell.AssignedRequestCount = model.Requests.Count(x => x.AssignedDeveloperUserId == currentUserId);
        model.Shell.ReviewRequestCount = model.Requests.Count(x => x.Status is "Kontrol Bekliyor" or "Kontrol Ediliyor");
        model.Shell.CompletedRequestCount = model.Requests.Count(x => x.Status == "Tamamlandi");
        model.Stats =
        [
            BuildStat("Acik Talepler", model.Shell.OpenRequestCount, "Uzerinde calisilabilecek aktif isler.", "info", "fa-list-check"),
            BuildStat("Bana Atananlar", model.Shell.AssignedRequestCount, "Sana planlanan veya dogrudan atanan talepler.", "success", "fa-user-check"),
            BuildStat("Kontrol Bekleyen", model.Shell.ReviewRequestCount, "Admin kontrolune gecmis teslimler.", "warning", "fa-magnifying-glass"),
            BuildStat("Tamamlananlar", model.Shell.CompletedRequestCount, "Bu panelde sonuclanan talepler.", "dark", "fa-flag-checkered")
        ];

        return model;
    }

    public async Task<(bool Success, string Message)> CreateRequestAsync(long currentUserId, DeveloperRequestCreateForm form, string? imageUrl, CancellationToken cancellationToken = default)
    {
        var title = (form.Title ?? string.Empty).Trim();
        var description = (form.Description ?? string.Empty).Trim();
        if (currentUserId <= 0 || title.Length < 6 || description.Length < 20)
        {
            return (false, "Baslik ve aciklama alanlarini daha detayli doldurun.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("""
            INSERT INTO dbo.gelistirme_talepleri
            (
                ana_talep_id, cevap_talep_id, kayit_tipi, kaynak_rol, olusturan_kullanici_id,
                baslik, aciklama, oncelik, durum, gorsel_url,
                son_hareket_tarihi, olusturulma_tarihi, guncellenme_tarihi
            )
            VALUES
            (
                NULL, NULL, N'talep', N'developer', @createdBy,
                @title, @description, @priority, N'Yeni', NULLIF(@imageUrl, N''),
                SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME()
            );
            """, connection);
        command.Parameters.AddWithValue("@createdBy", currentUserId);
        command.Parameters.AddWithValue("@title", title);
        command.Parameters.AddWithValue("@description", description);
        command.Parameters.AddWithValue("@priority", NormalizePriority(form.Priority));
        command.Parameters.AddWithValue("@imageUrl", imageUrl ?? string.Empty);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return (true, "Gelistirme talebi kaydedildi ve admin paneline dustu.");
    }

    public async Task<(bool Success, string Message)> AddDeveloperReplyAsync(long currentUserId, DeveloperRequestReplyForm form, string? imageUrl, CancellationToken cancellationToken = default)
    {
        if (form.RequestId <= 0 || string.IsNullOrWhiteSpace(form.Message))
        {
            return (false, "Talep guncellemesi icin mesaj zorunludur.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        var root = await LoadRootAsync(connection, transaction, form.RequestId, cancellationToken);
        if (root is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "Talep bulunamadi.");
        }

        if (root.CreatorUserId != currentUserId && root.AssignedDeveloperUserId != currentUserId)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "Bu talep icin guncelleme yetkiniz bulunmuyor.");
        }

        var nextStatus = NormalizeReplyStatus(form.ActionType, root.Status);
        await InsertActivityAsync(connection, transaction, root.RequestId, "yanit", "developer", currentUserId, root.AssignedDeveloperUserId, root.Title, form.Message.Trim(), root.Priority, nextStatus, root.PlannedStartDate, root.DueDate, imageUrl, cancellationToken);
        await UpdateRootAsync(connection, transaction, root.RequestId, root.Title, root.Description, root.Priority, nextStatus, root.AssignedDeveloperUserId, root.PlannedStartDate, root.DueDate, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (true, nextStatus == "Kontrol Bekliyor" ? "Talep kontrol icin admin paneline iletildi." : "Talep guncellemesi kaydedildi.");
    }

    public async Task<AdminDevelopmentRequestsPageViewModel> GetAdminPageAsync(string fullName, string email, string userRole, string? searchText = null, string? statusFilter = null, string? priorityFilter = null, long? developerFilterUserId = null, CancellationToken cancellationToken = default)
    {
        var model = new AdminDevelopmentRequestsPageViewModel
        {
            SearchText = (searchText ?? string.Empty).Trim(),
            StatusFilter = NormalizeStatusFilter(statusFilter),
            PriorityFilter = NormalizePriorityFilter(priorityFilter),
            DeveloperFilterUserId = developerFilterUserId
        };

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        model.Shell = await LoadAdminShellAsync(connection, fullName, email, userRole, cancellationToken);
        model.Developers = await LoadDeveloperOptionsAsync(connection, developerFilterUserId, cancellationToken);
        model.Requests = await LoadRequestCardsAsync(connection, 0, true, model.SearchText, model.StatusFilter, model.PriorityFilter, developerFilterUserId, cancellationToken);
        model.SummaryCards =
        [
            BuildAdminSummary("Toplam Talep", model.Requests.Count, "Tum aktif gelistirme kayitlari.", "primary"),
            BuildAdminSummary("Bekleyen", model.Requests.Count(x => x.Status is "Yeni" or "Planlandi"), "Planlama veya atama bekleyenler.", "warning"),
            BuildAdminSummary("Devam Eden", model.Requests.Count(x => x.Status is "Devam Ediyor" or "Kontrol Bekliyor" or "Kontrol Ediliyor"), "Calisma veya kontrol surecinde olanlar.", "success"),
            BuildAdminSummary("Tamamlanan", model.Requests.Count(x => x.Status == "Tamamlandi"), "Teslim edilmis talepler.", "dark")
        ];
        return model;
    }

    public async Task<(bool Success, string Message)> SaveAdminRequestAsync(long adminUserId, AdminDevelopmentRequestUpdateForm form, string? imageUrl, CancellationToken cancellationToken = default)
    {
        if (form.RequestId <= 0)
        {
            return (false, "Talep bulunamadi.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        var root = await LoadRootAsync(connection, transaction, form.RequestId, cancellationToken);
        if (root is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "Talep bulunamadi.");
        }

        var title = (form.Title ?? string.Empty).Trim();
        var description = (form.Description ?? string.Empty).Trim();
        var status = NormalizeStatus(form.Status);
        var priority = NormalizePriority(form.Priority);

        await UpdateRootAsync(connection, transaction, root.RequestId, title, description, priority, status, form.AssignedDeveloperUserId, form.PlannedStartDate, form.DueDate, cancellationToken);

        if (!string.IsNullOrWhiteSpace(form.ReplyMessage) || !string.IsNullOrWhiteSpace(imageUrl))
        {
            await InsertActivityAsync(connection, transaction, root.RequestId, "admin_yanit", "admin", adminUserId, form.AssignedDeveloperUserId, title, form.ReplyMessage.Trim(), priority, status, form.PlannedStartDate, form.DueDate, imageUrl, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return (true, "Gelistirme talebi guncellendi.");
    }

    public async Task<(bool Success, string Message)> DeleteRequestAsync(long adminUserId, long requestId, string? note, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        var root = await LoadRootAsync(connection, transaction, requestId, cancellationToken);
        if (root is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "Talep bulunamadi.");
        }

        if (!string.IsNullOrWhiteSpace(note))
        {
            await InsertActivityAsync(connection, transaction, root.RequestId, "admin_silme_notu", "admin", adminUserId, root.AssignedDeveloperUserId, root.Title, note.Trim(), root.Priority, "Reddedildi", root.PlannedStartDate, root.DueDate, null, cancellationToken);
        }

        await using var command = new SqlCommand("""
            UPDATE dbo.gelistirme_talepleri
            SET silindi_mi = 1,
                durum = N'Reddedildi',
                son_hareket_tarihi = SYSUTCDATETIME(),
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE id = @requestId OR ana_talep_id = @requestId;
            """, connection, transaction);
        command.Parameters.AddWithValue("@requestId", requestId);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (true, "Gelistirme talebi silindi.");
    }

    private async Task<List<DeveloperRequestCardViewModel>> LoadRequestCardsAsync(SqlConnection connection, long currentUserId, bool forAdmin, string? searchText, string statusFilter, string priorityFilter, long? developerFilterUserId, CancellationToken cancellationToken)
    {
        var items = new List<DeveloperRequestCardViewModel>();
        await using var command = new SqlCommand("""
            SELECT
                root.id,
                root.olusturan_kullanici_id,
                root.atanan_gelistirici_id,
                COALESCE(root.baslik, N''),
                COALESCE(root.aciklama, N''),
                COALESCE(root.oncelik, N'Orta'),
                COALESCE(root.durum, N'Yeni'),
                root.planlanan_baslangic_tarihi,
                root.hedef_bitis_tarihi,
                root.tamamlanma_tarihi,
                root.gorsel_url,
                root.olusturulma_tarihi,
                root.son_hareket_tarihi,
                COALESCE(creator.ad_soyad, N'Developer'),
                COALESCE(assignee.ad_soyad, N'')
            FROM dbo.gelistirme_talepleri AS root
            INNER JOIN dbo.users AS creator ON creator.id = root.olusturan_kullanici_id
            LEFT JOIN dbo.users AS assignee ON assignee.id = root.atanan_gelistirici_id
            WHERE root.ana_talep_id IS NULL
              AND root.silindi_mi = 0
              AND (@forAdmin = 1 OR root.olusturan_kullanici_id = @currentUserId OR root.atanan_gelistirici_id = @currentUserId)
              AND (@search = N'' OR root.baslik LIKE N'%' + @search + N'%' OR root.aciklama LIKE N'%' + @search + N'%')
              AND (@statusFilter = N'all' OR root.durum = @statusFilter)
              AND (@priorityFilter = N'all' OR root.oncelik = @priorityFilter)
              AND (@developerFilterUserId IS NULL OR root.atanan_gelistirici_id = @developerFilterUserId)
            ORDER BY root.son_hareket_tarihi DESC, root.id DESC;
            """, connection);
        command.Parameters.AddWithValue("@forAdmin", forAdmin ? 1 : 0);
        command.Parameters.AddWithValue("@currentUserId", currentUserId);
        command.Parameters.AddWithValue("@search", searchText ?? string.Empty);
        command.Parameters.AddWithValue("@statusFilter", statusFilter);
        command.Parameters.AddWithValue("@priorityFilter", priorityFilter);
        command.Parameters.AddWithValue("@developerFilterUserId", (object?)developerFilterUserId ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            long? assignedUserId = reader.IsDBNull(2) ? (long?)null : reader.GetInt64(2);
            var requestId = reader.GetInt64(0);
            var priority = reader.GetString(5);
            var status = reader.GetString(6);
            items.Add(new DeveloperRequestCardViewModel
            {
                RequestId = requestId,
                CreatorUserId = reader.GetInt64(1),
                AssignedDeveloperUserId = assignedUserId,
                Title = reader.GetString(3),
                Description = reader.GetString(4),
                Priority = priority,
                PriorityToneClass = GetPriorityTone(priority),
                Status = status,
                StatusToneClass = GetStatusTone(status),
                PlannedStartDateText = FormatDate(reader, 7),
                DueDateText = FormatDate(reader, 8),
                CompletedAtText = FormatDateTime(reader, 9),
                ImageUrl = reader.IsDBNull(10) ? null : reader.GetString(10),
                CreatedAtText = FormatDateTime(reader, 11) ?? string.Empty,
                LastActivityText = FormatDateTime(reader, 12) ?? string.Empty,
                CreatorName = reader.GetString(13),
                AssignedDeveloperName = reader.GetString(14),
                IsAssignedToCurrentDeveloper = assignedUserId == currentUserId && currentUserId > 0,
                IsCreatedByCurrentDeveloper = reader.GetInt64(1) == currentUserId && currentUserId > 0,
                CanDeveloperReply = !forAdmin && (reader.GetInt64(1) == currentUserId || assignedUserId == currentUserId),
                CanAdminManage = forAdmin,
                ReplyForm = new DeveloperRequestReplyForm { RequestId = requestId },
                AdminForm = new AdminDevelopmentRequestUpdateForm
                {
                    RequestId = requestId,
                    Title = reader.GetString(3),
                    Description = reader.GetString(4),
                    Priority = priority,
                    Status = status,
                    AssignedDeveloperUserId = assignedUserId,
                    PlannedStartDate = ParseDateOnly(reader, 7),
                    DueDate = ParseDateOnly(reader, 8)
                }
            });
        }

        await reader.CloseAsync();
        if (items.Count == 0)
        {
            return items;
        }

        var ids = string.Join(",", items.Select(x => x.RequestId.ToString(CultureInfo.InvariantCulture)));
        await using var activityCommand = new SqlCommand($"""
            SELECT
                activity.id,
                activity.ana_talep_id,
                activity.kayit_tipi,
                activity.kaynak_rol,
                COALESCE(u.ad_soyad, N'Ekip'),
                COALESCE(activity.aciklama, N''),
                COALESCE(activity.durum, N''),
                COALESCE(activity.oncelik, N''),
                activity.gorsel_url,
                activity.olusturulma_tarihi
            FROM dbo.gelistirme_talepleri AS activity
            LEFT JOIN dbo.users AS u ON u.id = activity.olusturan_kullanici_id
            WHERE activity.ana_talep_id IN ({ids})
              AND activity.silindi_mi = 0
            ORDER BY activity.olusturulma_tarihi ASC, activity.id ASC;
            """, connection);
        await using var activityReader = await activityCommand.ExecuteReaderAsync(cancellationToken);
        var itemMap = items.ToDictionary(x => x.RequestId);
        while (await activityReader.ReadAsync(cancellationToken))
        {
            if (!itemMap.TryGetValue(activityReader.GetInt64(1), out var card))
            {
                continue;
            }

            card.Activities.Add(new DeveloperRequestActivityViewModel
            {
                ActivityId = activityReader.GetInt64(0),
                ActivityType = activityReader.GetString(2),
                ActivityLabel = GetActivityLabel(activityReader.GetString(2)),
                SourceRole = activityReader.GetString(3),
                SourceRoleLabel = GetSourceRoleLabel(activityReader.GetString(3)),
                SourceName = activityReader.GetString(4),
                Message = activityReader.GetString(5),
                Status = activityReader.GetString(6),
                Priority = activityReader.GetString(7),
                ImageUrl = activityReader.IsDBNull(8) ? null : activityReader.GetString(8),
                CreatedAtText = FormatDateTime(activityReader, 9) ?? string.Empty
            });
        }

        return items;
    }

    private async Task<AdminShellViewModel> LoadAdminShellAsync(SqlConnection connection, string fullName, string email, string userRole, CancellationToken cancellationToken)
    {
        async Task<int> CountAsync(string sql)
        {
            try
            {
                await using var countCommand = new SqlCommand(sql, connection);
                var scalar = await countCommand.ExecuteScalarAsync(cancellationToken);
                return scalar is null || scalar == DBNull.Value ? 0 : Convert.ToInt32(scalar, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }

        return new AdminShellViewModel
        {
            FullName = fullName,
            Email = email,
            UserRole = userRole,
            PanelTitle = "Gelistirme Talepleri",
            PanelSubtitle = "Developer ekibinden gelen proje taleplerini planla, cevapla ve son durumu takip et.",
            PendingPartnerApplications = await CountAsync("SELECT COUNT(*) FROM partner_basvurulari WHERE durum = N'Bekliyor';"),
            PendingCompanyApplications = await CountAsync("SELECT COUNT(*) FROM firma_basvurulari WHERE durum = N'Bekliyor';"),
            UnreadNotifications = await CountAsync("SELECT COUNT(*) FROM bildirim_loglari WHERE durum = N'Bekliyor';"),
            PendingReviews = await CountAsync("SELECT COUNT(*) FROM yorumlar WHERE onay_durumu LIKE N'Bekliyor%';"),
            CriticalLogs = await CountAsync("SELECT COUNT(*) FROM log_kayitlari WHERE seviye IN (N'Kritik', N'Critical', N'Error');")
        };
    }

    private async Task<List<DeveloperUserOptionViewModel>> LoadDeveloperOptionsAsync(SqlConnection connection, long? selectedUserId, CancellationToken cancellationToken)
    {
        var developers = new List<DeveloperUserOptionViewModel>();
        await using var command = new SqlCommand("""
            SELECT d.id, d.ad_soyad, d.eposta
            FROM (
                SELECT DISTINCT
                    u.id,
                    COALESCE(u.ad_soyad, N'Developer') AS ad_soyad,
                    COALESCE(u.eposta, N'') AS eposta
                FROM dbo.users AS u
                LEFT JOIN dbo.kullanici_rolleri AS ur ON ur.kullanici_id = u.id AND ur.bitis_tarihi IS NULL
                LEFT JOIN dbo.roller AS r ON r.id = ur.rol_id
                WHERE LOWER(COALESCE(u.rol, N'')) = N'developer'
                   OR LOWER(COALESCE(r.rol_kodu, N'')) = N'developer'
            ) d
            ORDER BY d.ad_soyad, d.eposta;
            """, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var userId = reader.GetInt64(0);
            developers.Add(new DeveloperUserOptionViewModel
            {
                UserId = userId,
                FullName = reader.GetString(1),
                Email = reader.GetString(2),
                IsSelected = selectedUserId.HasValue && selectedUserId.Value == userId
            });
        }

        return developers;
    }

    private static DeveloperStatCardViewModel BuildStat(string label, int value, string description, string toneClass, string iconClass)
        => new()
        {
            Label = label,
            Value = value.ToString("N0", CultureInfo.GetCultureInfo("tr-TR")),
            Description = description,
            ToneClass = toneClass,
            IconClass = iconClass
        };

    private static AdminSummaryCardViewModel BuildAdminSummary(string label, int value, string description, string toneClass)
        => new()
        {
            Label = label,
            Value = value.ToString("N0", CultureInfo.GetCultureInfo("tr-TR")),
            Description = description,
            ToneClass = toneClass,
            IconClass = toneClass switch
            {
                "warning" => "fa-hourglass-half",
                "success" => "fa-person-running",
                "dark" => "fa-flag-checkered",
                _ => "fa-layer-group"
            }
        };

    private static async Task InsertActivityAsync(SqlConnection connection, SqlTransaction transaction, long requestId, string recordType, string sourceRole, long createdByUserId, long? assignedDeveloperUserId, string title, string description, string priority, string status, DateOnly? plannedStartDate, DateOnly? dueDate, string? imageUrl, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("""
            INSERT INTO dbo.gelistirme_talepleri
            (
                ana_talep_id, cevap_talep_id, kayit_tipi, kaynak_rol, olusturan_kullanici_id,
                atanan_gelistirici_id, baslik, aciklama, oncelik, durum,
                planlanan_baslangic_tarihi, hedef_bitis_tarihi, gorsel_url,
                son_hareket_tarihi, olusturulma_tarihi, guncellenme_tarihi
            )
            VALUES
            (
                @requestId, @requestId, @recordType, @sourceRole, @createdByUserId,
                @assignedDeveloperUserId, @title, @description, @priority, @status,
                @plannedStartDate, @dueDate, NULLIF(@imageUrl, N''),
                SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME()
            );
            """, connection, transaction);
        command.Parameters.AddWithValue("@requestId", requestId);
        command.Parameters.AddWithValue("@recordType", recordType);
        command.Parameters.AddWithValue("@sourceRole", sourceRole);
        command.Parameters.AddWithValue("@createdByUserId", createdByUserId);
        command.Parameters.AddWithValue("@assignedDeveloperUserId", (object?)assignedDeveloperUserId ?? DBNull.Value);
        command.Parameters.AddWithValue("@title", title);
        command.Parameters.AddWithValue("@description", description);
        command.Parameters.AddWithValue("@priority", priority);
        command.Parameters.AddWithValue("@status", status);
        command.Parameters.AddWithValue("@plannedStartDate", (object?)plannedStartDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
        command.Parameters.AddWithValue("@dueDate", (object?)dueDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
        command.Parameters.AddWithValue("@imageUrl", imageUrl ?? string.Empty);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpdateRootAsync(SqlConnection connection, SqlTransaction transaction, long requestId, string title, string description, string priority, string status, long? assignedDeveloperUserId, DateOnly? plannedStartDate, DateOnly? dueDate, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("""
            UPDATE dbo.gelistirme_talepleri
            SET baslik = @title,
                aciklama = @description,
                oncelik = @priority,
                durum = @status,
                atanan_gelistirici_id = @assignedDeveloperUserId,
                planlanan_baslangic_tarihi = @plannedStartDate,
                hedef_bitis_tarihi = @dueDate,
                tamamlanma_tarihi = CASE WHEN @status = N'Tamamlandi' THEN COALESCE(tamamlanma_tarihi, SYSUTCDATETIME()) ELSE NULL END,
                son_hareket_tarihi = SYSUTCDATETIME(),
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE id = @requestId;
            """, connection, transaction);
        command.Parameters.AddWithValue("@requestId", requestId);
        command.Parameters.AddWithValue("@title", title);
        command.Parameters.AddWithValue("@description", description);
        command.Parameters.AddWithValue("@priority", priority);
        command.Parameters.AddWithValue("@status", status);
        command.Parameters.AddWithValue("@assignedDeveloperUserId", (object?)assignedDeveloperUserId ?? DBNull.Value);
        command.Parameters.AddWithValue("@plannedStartDate", (object?)plannedStartDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
        command.Parameters.AddWithValue("@dueDate", (object?)dueDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<RootRequestRecord?> LoadRootAsync(SqlConnection connection, SqlTransaction transaction, long requestId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("""
            SELECT TOP (1)
                id,
                olusturan_kullanici_id,
                atanan_gelistirici_id,
                COALESCE(baslik, N''),
                COALESCE(aciklama, N''),
                COALESCE(oncelik, N'Orta'),
                COALESCE(durum, N'Yeni'),
                planlanan_baslangic_tarihi,
                hedef_bitis_tarihi
            FROM dbo.gelistirme_talepleri
            WHERE id = @requestId
              AND ana_talep_id IS NULL
              AND silindi_mi = 0;
            """, connection, transaction);
        command.Parameters.AddWithValue("@requestId", requestId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new RootRequestRecord
        {
            RequestId = reader.GetInt64(0),
            CreatorUserId = reader.GetInt64(1),
            AssignedDeveloperUserId = reader.IsDBNull(2) ? null : reader.GetInt64(2),
            Title = reader.GetString(3),
            Description = reader.GetString(4),
            Priority = reader.GetString(5),
            Status = reader.GetString(6),
            PlannedStartDate = ParseDateOnly(reader, 7),
            DueDate = ParseDateOnly(reader, 8)
        };
    }

    private static string NormalizePriority(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "kritik" => "Kritik",
        "yuksek" => "Yuksek",
        "dusuk" => "Dusuk",
        _ => "Orta"
    };

    private static string NormalizePriorityFilter(string? value) => string.IsNullOrWhiteSpace(value)
        ? "all"
        : NormalizePriority(value) switch
        {
            "Kritik" => "Kritik",
            "Yuksek" => "Yuksek",
            "Orta" => "Orta",
            "Dusuk" => "Dusuk",
            _ => "all"
        };

    private static string NormalizeStatus(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "planlandi" => "Planlandi",
        "devam ediyor" or "devam_ediyor" => "Devam Ediyor",
        "kontrol bekliyor" or "kontrol_bekliyor" => "Kontrol Bekliyor",
        "kontrol ediliyor" or "kontrol_ediliyor" => "Kontrol Ediliyor",
        "tamamlandi" => "Tamamlandi",
        "reddedildi" => "Reddedildi",
        "ek gelistirme istendi" or "ek_gelistirme_istendi" => "Ek Gelistirme Istendi",
        _ => "Yeni"
    };

    private static string NormalizeStatusFilter(string? value) => string.IsNullOrWhiteSpace(value) ? "all" : NormalizeStatus(value);

    private static string NormalizeReplyStatus(string? actionType, string currentStatus) => actionType?.Trim().ToLowerInvariant() switch
    {
        "ready_for_review" => "Kontrol Bekliyor",
        "in_progress" => "Devam Ediyor",
        "needs_revision" => "Ek Gelistirme Istendi",
        _ => currentStatus
    };

    private static string GetPriorityTone(string priority) => priority switch
    {
        "Kritik" => "danger",
        "Yuksek" => "warning",
        "Dusuk" => "muted",
        _ => "info"
    };

    private static string GetStatusTone(string status) => status switch
    {
        "Tamamlandi" => "success",
        "Reddedildi" => "danger",
        "Devam Ediyor" => "info",
        "Kontrol Bekliyor" or "Kontrol Ediliyor" => "warning",
        "Ek Gelistirme Istendi" => "accent",
        _ => "primary"
    };

    private static string GetActivityLabel(string type) => type switch
    {
        "admin_yanit" => "Admin Guncellemesi",
        "admin_silme_notu" => "Silme Notu",
        "yanit" => "Developer Cevabi",
        _ => "Guncelleme"
    };

    private static string GetSourceRoleLabel(string role) => role switch
    {
        "admin" => "Admin",
        "developer" => "Developer",
        _ => "Sistem"
    };

    private static bool IsClosedStatus(string? status) => status is "Tamamlandi" or "Reddedildi";

    private static string? FormatDate(SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : Convert.ToDateTime(reader.GetValue(ordinal), CultureInfo.InvariantCulture).ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"));

    private static string? FormatDateTime(SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : Convert.ToDateTime(reader.GetValue(ordinal), CultureInfo.InvariantCulture).ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"));

    private static DateOnly? ParseDateOnly(SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : DateOnly.FromDateTime(Convert.ToDateTime(reader.GetValue(ordinal), CultureInfo.InvariantCulture));

    private sealed class RootRequestRecord
    {
        public long RequestId { get; init; }
        public long CreatorUserId { get; init; }
        public long? AssignedDeveloperUserId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Priority { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateOnly? PlannedStartDate { get; init; }
        public DateOnly? DueDate { get; init; }
    }
}
