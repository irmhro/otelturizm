using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class AdminRbacService : IAdminRbacService
{
    private readonly string _connectionString;
    private readonly ILogger<AdminRbacService> _logger;

    public AdminRbacService(IConfiguration configuration, ILogger<AdminRbacService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _logger = logger;
    }

    public async Task<HashSet<string>> GetPermissionsAsync(long adminUserId, string userRole, CancellationToken cancellationToken = default)
    {
        // Backward compatible: eski sistemde "admin/superadmin" full yetkili kabul edilir.
        if (string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(userRole, "superadmin", StringComparison.OrdinalIgnoreCase))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "*" };
        }

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = """
                SELECT DISTINCT y.[YETKI_CODE]
                FROM [dbo].[ADMIN_KULLANICI_ROLLER] ur
                INNER JOIN [dbo].[ADMIN_ROL_YETKILER] ry ON ry.[ROL_CODE] = ur.[ROL_CODE] AND ry.[ACTIVE] = 1
                INNER JOIN [dbo].[ADMIN_YETKILER] y ON y.[YETKI_CODE] = ry.[YETKI_CODE] AND y.[ACTIVE] = 1
                WHERE ur.[ADMIN_KULLANICI_ID] = @uid AND ur.[ACTIVE] = 1;
                """;

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@uid", adminUserId);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                set.Add(reader.GetString(0));
            }
        }
        catch (SqlException ex)
        {
            // RBAC tabloları yoksa tam yetki davranışını bozmayalım.
            _logger.LogWarning(ex, "Admin RBAC tabloları okunamadı; fallback full yetki uygulanacak.");
            set.Add("*");
        }

        if (set.Count == 0)
        {
            // Sessiz fallback: rol ataması yoksa hiçbir şey yerine minimum izni de vermek riskli.
            // Bu yüzden mevcut admin girişleri için pratikte userRole admin olacak. Burada güvenli tarafta kalıyoruz:
            // set boş kalırsa menü gizleme devreye girer ama endpoint guard yoksa sorun olur.
            // Endpoint tarafında da aynı servis kullanıldığı için tutarlı olacak.
        }

        return set;
    }

    public async Task<bool> HasPermissionAsync(long adminUserId, string userRole, string permissionCode, CancellationToken cancellationToken = default)
    {
        var set = await GetPermissionsAsync(adminUserId, userRole, cancellationToken);
        return set.Contains("*") || set.Contains(permissionCode);
    }
}

