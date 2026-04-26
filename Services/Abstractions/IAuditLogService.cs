using Microsoft.AspNetCore.Http;

namespace otelturizmnew.Services.Abstractions;

public interface IAuditLogService
{
    Task TryLogApiRequestAsync(HttpContext context, int statusCode, long elapsedMs, CancellationToken cancellationToken = default);
    Task TryLogExceptionAsync(HttpContext context, Exception exception, CancellationToken cancellationToken = default);
    Task TryLogAdminActionAsync(long adminUserId, string actionType, string targetTable, string? targetId, string? note, string? ipAddress, CancellationToken cancellationToken = default);
}

