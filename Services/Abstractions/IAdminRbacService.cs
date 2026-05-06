using otelturizmnew.Models.Paneller.Admin;

namespace otelturizmnew.Services.Abstractions;

public interface IAdminRbacService
{
    Task<HashSet<string>> GetPermissionsAsync(long adminUserId, string userRole, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(long adminUserId, string userRole, string permissionCode, CancellationToken cancellationToken = default);
}

